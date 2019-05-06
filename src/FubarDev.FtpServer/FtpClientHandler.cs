// <copyright file="FtpClientHandler.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using FubarDev.FtpServer.Authentication;
using FubarDev.FtpServer.Features;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FubarDev.FtpServer
{
    public class FtpClientHandler : IDisposable, IFtpServerHost
    {
        [NotNull]
        private readonly SemaphoreSlim _responseSenderPaused = new SemaphoreSlim(0, 1);
        [NotNull]
        private readonly SemaphoreSlim _responseSenderRunning = new SemaphoreSlim(0, 1);
        [NotNull]
        private readonly CancellationTokenSource _serverShutdownRequestedCts = new CancellationTokenSource();
        [NotNull]
        private readonly CancellationTokenSource _clientClosedCts = new CancellationTokenSource();
        [CanBeNull]
        private readonly IDisposable _loggerScope;
        [NotNull]
        private readonly TcpClient _socket;
        [NotNull]
        private readonly IFtpCommandMultiplexer _commandMultiplexer;

        [NotNull]
        private readonly ISslStreamWrapperFactory _sslStreamWrapperFactory;

        [CanBeNull]
        private readonly X509Certificate2 _serverCertificate;

        [NotNull]
        private NetworkStream _socketStream;
        [CanBeNull]
        private Task _processTask;
        [NotNull]
        private Stream _connectionStream;
        private bool _closed;

        public FtpClientHandler(
            [NotNull] TcpClient socket,
            [NotNull] FtpConnectionState state,
            [NotNull] IFtpCommandMultiplexer multiplexer,
            [NotNull] ISslStreamWrapperFactory sslStreamWrapperFactory,
            [NotNull] IOptions<AuthTlsOptions> authTlsOptions)
        {
            state.Features.Set<IFtpConnectionLifetimeFeature>(
                new FtpConnectionLifetimeFeature(_clientClosedCts));

            var remoteAddress = state.RemoteAddress;

            var properties = new Dictionary<string, object>
            {
                ["RemoteAddress"] = remoteAddress.ToString(true),
                ["RemoteIp"] = remoteAddress.IPAddress?.ToString(),
                ["RemotePort"] = remoteAddress.Port,
            };

            var logger = state.Features.Get<IConnectionFeature>().Logger;
            _loggerScope = logger?.BeginScope(properties);
            _socket = socket;
            _connectionStream = _socketStream = socket.GetStream();
            _serverCertificate = authTlsOptions.Value.ServerCertificate;
            _commandMultiplexer = multiplexer;
            _sslStreamWrapperFactory = sslStreamWrapperFactory;

            Log = logger;
            State = state;
        }

        public ILogger Log { get; }

        public FtpConnectionState State { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_closed)
            {
                Close();
            }

            _socket.Dispose();
            _clientClosedCts.Dispose();
            _loggerScope?.Dispose();
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            _clientClosedCts.Cancel(true);
            _closed = true;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _processTask = ProcessMessagesAsync(_clientClosedCts.Token);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Close();
            return _processTask ?? Task.CompletedTask;
        }

        private async Task ProcessMessagesAsync(CancellationToken clientClosed)
        {
            Log?.LogInformation($"Connected from {State.RemoteAddress.ToString(true)}");

            var serverCommandChannel = Channel.CreateUnbounded<FtpServerCommand>();
            var serverStatusChannel = Channel.CreateUnbounded<FtpServerStatus>();
            var ftpCommandChannel = Channel.CreateUnbounded<FtpCommand>();
            var ftpResponseChannel = Channel.CreateUnbounded<IFtpResponse>();
            var serverCommandListenerTask = StartServerCommandListener(
                serverCommandChannel,
                serverStatusChannel,
                clientClosed);
            var commandCollectorTask = StartCommandCollectorAsync(ftpCommandChannel, clientClosed);
            var responseSenderTask = StartResponseSenderAsync(
                ftpResponseChannel,
                clientClosed);

            // Send initial response
            var initialResponse = new FtpResponse(220, "FTP Server Ready");
            await ftpResponseChannel.Writer.WriteAsync(initialResponse, clientClosed)
               .ConfigureAwait(false);

            // Start command multiplexer afterwards!
            var multiplexerTask = _commandMultiplexer.ExecuteAsync(
                serverCommandChannel,
                serverStatusChannel,
                ftpCommandChannel,
                ftpResponseChannel,
                clientClosed);

            // Wait until communication channels are closed.
            Task.WaitAny(
                commandCollectorTask,
                responseSenderTask);

            _clientClosedCts.Cancel();

            // Wait until remaining services are closed.
            Task.WaitAll(
                commandCollectorTask,
                responseSenderTask,
                multiplexerTask,
                serverCommandListenerTask);

            _socket.Dispose();
        }

        private async Task ExecuteServerCommandAsync(
            FtpServerCommand ftpServerCommand,
            ChannelWriter<FtpServerStatus> serverStatusWriter,
            CancellationToken clientClosed)
        {
            switch (ftpServerCommand)
            {
                case FtpServerCommand.Shutdown:
                    _serverShutdownRequestedCts.Cancel();
                    serverStatusWriter.Complete();
                    break;
                case FtpServerCommand.EnableTls:
                    await EnableTls(
                            serverStatusWriter,
                            clientClosed)
                       .ConfigureAwait(false);
                    break;
                case FtpServerCommand.DisableTls:
                    await DisableTls(serverStatusWriter, clientClosed)
                       .ConfigureAwait(false);
                    break;
                case FtpServerCommand.ResumeResponseSender:
                    _responseSenderPaused.Release();
                    break;
            }
        }

        private async Task EnableTls(
            ChannelWriter<FtpServerStatus> serverStatusWriter,
            CancellationToken clientClosed)
        {
            if (_serverCertificate == null)
            {
                await serverStatusWriter.WriteAsync(
                        FtpServerStatus.TlsEnableErrorNotConfigured,
                        clientClosed)
                   .ConfigureAwait(false);
                return;
            }

            await DisableTls(serverStatusWriter, clientClosed)
               .ConfigureAwait(false);

            try
            {
                var sslStream = await _sslStreamWrapperFactory.WrapStreamAsync(
                        _connectionStream,
                        true,
                        _serverCertificate,
                        clientClosed)
                   .ConfigureAwait(false);

                _connectionStream = sslStream;
            }
            catch (Exception ex)
            {
                Log?.LogWarning(ex, "TLS connection couldn't be established: {error}", ex.Message);
                await serverStatusWriter.WriteAsync(
                        FtpServerStatus.TlsEnableError,
                        clientClosed)
                   .ConfigureAwait(false);
                return;
            }

            await serverStatusWriter.WriteAsync(
                    FtpServerStatus.TlsEnabled,
                    clientClosed)
               .ConfigureAwait(false);
        }

        private async Task DisableTls(
            ChannelWriter<FtpServerStatus> serverStatusWriter,
            CancellationToken clientClosed)
        {
            await _responseSenderRunning.WaitAsync(clientClosed)
               .ConfigureAwait(false);

            if (_connectionStream == _socketStream)
            {
                await serverStatusWriter.WriteAsync(
                        FtpServerStatus.TlsWasDisabled,
                        clientClosed)
                   .ConfigureAwait(false);
                return;
            }

            await _sslStreamWrapperFactory.CloseStreamAsync(
                _connectionStream,
                clientClosed);

            _connectionStream = _socketStream;

            await serverStatusWriter.WriteAsync(
                    FtpServerStatus.TlsDisabled,
                    clientClosed)
               .ConfigureAwait(false);
        }

        private async Task StartServerCommandListener(
            ChannelReader<FtpServerCommand> serverCommandChannelReader,
            ChannelWriter<FtpServerStatus> serverStatusWriter,
            CancellationToken clientClosed)
        {
            var serverShutdownRequested = _serverShutdownRequestedCts.Token;
            try
            {
                var readAllowed = true;
                while (readAllowed
                       && !clientClosed.IsCancellationRequested
                       && !serverShutdownRequested.IsCancellationRequested)
                {
                    readAllowed = await serverCommandChannelReader.WaitToReadAsync(clientClosed)
                       .ConfigureAwait(false);
                    if (readAllowed)
                    {
                        while (serverCommandChannelReader.TryRead(out var serverCommand))
                        {
                            await ExecuteServerCommandAsync(
                                    serverCommand,
                                    serverStatusWriter,
                                    clientClosed)
                               .ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore the OperationCanceledException
                // This is normal during disconnects
            }
            catch (Exception ex)
            {
                Log?.LogError(ex, "Failed to read server command");
            }
            finally
            {
                Log?.LogInformation($"No more server commands will be processed");
            }
        }

        private async Task StartResponseSenderAsync(
            ChannelReader<IFtpResponse> ftpResponseReader,
            CancellationToken clientClosed)
        {
            var serverShutdownRequested = _serverShutdownRequestedCts.Token;
            try
            {
                var readAllowed = true;
                while (readAllowed
                       && !clientClosed.IsCancellationRequested
                       && !serverShutdownRequested.IsCancellationRequested)
                {
                    readAllowed = await ftpResponseReader.WaitToReadAsync(clientClosed)
                       .ConfigureAwait(false);
                    if (readAllowed)
                    {
                        var paused = false;
                        while (ftpResponseReader.TryRead(out var response))
                        {
                            await WriteAsync(response, clientClosed)
                               .ConfigureAwait(false);
                            if (response.PauseConnection)
                            {
                                paused = true;
                                break;
                            }
                        }

                        if (paused)
                        {
                            _responseSenderRunning.Release();
                            await _responseSenderPaused.WaitAsync(TimeSpan.FromSeconds(10), clientClosed)
                               .ConfigureAwait(false);
                        }
                    }
                }

                if (readAllowed)
                {
                    while (ftpResponseReader.TryRead(out var response))
                    {
                        await WriteAsync(response, clientClosed)
                           .ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore the OperationCanceledException
                // This is normal during disconnects
            }
            catch (Exception ex)
            {
                Log?.LogError(ex, "Failed to read response");
            }
            finally
            {
                Log?.LogInformation($"No more responses will be sent to {State.RemoteAddress.ToString(true)}");
            }
        }

        private async Task StartCommandCollectorAsync(
            ChannelWriter<FtpCommand> ftpCommandWriter,
            CancellationToken clientClosed)
        {
            var serverShutdownRequested = _serverShutdownRequestedCts.Token;
            var collector = new FtpCommandCollector(() => State.Encoding);
            var buffer = new byte[1024];
            try
            {
                var writeAllowed = true;
                while (writeAllowed
                       && !clientClosed.IsCancellationRequested
                       && !serverShutdownRequested.IsCancellationRequested)
                {
                    var bytesRead = await _connectionStream
                       .ReadAsync(buffer, 0, buffer.Length, clientClosed)
                       .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    var commands = collector.Collect(buffer.AsSpan(0, bytesRead));
                    foreach (var command in commands)
                    {
                        writeAllowed = await ftpCommandWriter.WaitToWriteAsync(clientClosed)
                           .ConfigureAwait(false);
                        if (!writeAllowed)
                        {
                            break;
                        }

                        await ftpCommandWriter.WriteAsync(command, clientClosed)
                           .ConfigureAwait(false);
                    }
                }

                ftpCommandWriter.Complete();
            }
            catch (OperationCanceledException)
            {
                // Ignore the OperationCanceledException
                // This is normal during disconnects
            }
            catch (Exception ex)
            {
                Log?.LogError(ex, "Failed to receive commands");
                ftpCommandWriter.Complete(ex);
            }
            finally
            {
                Log?.LogInformation($"No more commands will be received from {State.RemoteAddress.ToString(true)}");
            }
        }

        /// <summary>
        /// Writes a FTP response to a client.
        /// </summary>
        /// <param name="response">The response to write to the client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        private async Task WriteAsync(
            IFtpResponse response,
            CancellationToken cancellationToken)
        {
            if (_closed)
            {
                return;
            }

            Log?.Log(response);
            var line = await response.GetNextLineAsync(null, cancellationToken)
               .ConfigureAwait(false);
            for (; ;)
            {
                if (line.HasText)
                {
                    var data = State.Encoding.GetBytes($"{response}\r\n");
                    await _connectionStream.WriteAsync(data, 0, data.Length, cancellationToken)
                       .ConfigureAwait(false);
                }

                if (!line.HasMoreData)
                {
                    break;
                }

                line = await response.GetNextLineAsync(line.Token, cancellationToken)
                   .ConfigureAwait(false);
            }
        }
    }
}
