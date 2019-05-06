// <copyright file="FtpCommandMultiplexer.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using FubarDev.FtpServer.CommandHandlers;
using FubarDev.FtpServer.Features;
using FubarDev.FtpServer.FileSystem.Error;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace FubarDev.FtpServer
{
    public class FtpCommandMultiplexer : IFtpCommandMultiplexer
    {
        [NotNull]
        private readonly IFtpLoginStateMachine _loginStateMachine;

        [NotNull]
        private readonly IFtpContextAccessor _ftpContextAccessor;

        [NotNull]
        private readonly FtpConnectionState _state;

        [NotNull]
        private readonly FtpRequestDelegate _requestDelegate;

        [NotNull]
        private readonly IServerStatusEventFeature _serverStatusEventFeature;

        private CancellationTokenRegistration _activeBackgroundCommandCtRegistration;

        [CanBeNull]
        private Task<IFtpResponse> _activeBackgroundCommandTask;

        public FtpCommandMultiplexer(
            [NotNull, ItemNotNull] IEnumerable<IFtpCommandHandler> commandHandlers,
            [NotNull, ItemNotNull] IEnumerable<IFtpMiddleware> middlewares,
            [NotNull] IFtpLoginStateMachine loginStateMachine,
            [NotNull] IFtpContextAccessor ftpContextAccessor,
            [NotNull] FtpConnectionState state,
            [CanBeNull] ILogger<FtpCommandMultiplexer> logger = null)
        {
            _loginStateMachine = loginStateMachine;
            _ftpContextAccessor = ftpContextAccessor;
            _state = state;
            Log = logger;
            var commandHandlersList = commandHandlers.ToList();
            var dict = commandHandlersList
               .SelectMany(x => x.Names, (item, name) => new { Name = name, Item = item })
               .ToLookup(x => x.Name, x => x.Item, StringComparer.OrdinalIgnoreCase)
               .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

            CommandHandlers = dict;

            var nextStep = new FtpRequestDelegate(DispatchCommandAsync);
            foreach (var middleware in middlewares.Reverse())
            {
                var tempStep = nextStep;
                nextStep = (context) => middleware.InvokeAsync(context, tempStep);
            }

            _serverStatusEventFeature = new ServerStatusEventFeature(this);
            _requestDelegate = nextStep;

            state.Features.Set(_serverStatusEventFeature);
        }

        public IReadOnlyDictionary<string, IFtpCommandHandler> CommandHandlers { get; }

        private ILogger<FtpCommandMultiplexer> Log { get; }

        /// <inheritdoc />
        public async Task ExecuteAsync(
            ChannelWriter<FtpServerCommand> serverCommandWriter,
            ChannelReader<FtpServerStatus> serverStatusReader,
            ChannelReader<FtpCommand> ftpCommandReader,
            ChannelWriter<IFtpResponse> ftpResponseWriter,
            CancellationToken cancellationToken)
        {
            _state.Features.Set<ITlsConnectionFeature>(
                new TlsConnectionFeature(
                    serverCommandWriter,
                    _serverStatusEventFeature,
                    cancellationToken));
            try
            {
                var serverStatusListenerTask = StartServerStatusListener(serverStatusReader, cancellationToken);
                var readAllowed = true;
                while (readAllowed && !cancellationToken.IsCancellationRequested)
                {
                    Task<bool> readTask = null;
                    for (;;)
                    {
                        if (readTask == null)
                        {
                            readTask = ftpCommandReader.WaitToReadAsync(cancellationToken).AsTask();
                        }

                        var tasks = new List<Task>() {readTask};
                        if (_activeBackgroundCommandTask != null)
                        {
                            tasks.Add(_activeBackgroundCommandTask);
                        }

                        Debug.WriteLine($"Waiting for {tasks.Count} tasks");
                        var completedTask = Task.WaitAny(tasks.ToArray(), cancellationToken);
                        Debug.WriteLine($"Task {completedTask} completed");
                        if (completedTask == 1)
                        {
                            try
                            {
                                var response = _activeBackgroundCommandTask?.Result;
                                if (response != null)
                                {
                                    await ftpResponseWriter.WriteAsync(response, cancellationToken)
                                       .ConfigureAwait(false);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                await ftpResponseWriter
                                   .WriteAsync(
                                        new FtpResponse(500, T("Syntax error, command unrecognized.")),
                                        cancellationToken)
                                   .ConfigureAwait(false);
                            }
                            finally
                            {
                                _activeBackgroundCommandCtRegistration.Dispose();
                                _activeBackgroundCommandTask = null;
                            }
                        }
                        else
                        {
                            readAllowed = await ftpCommandReader.WaitToReadAsync(cancellationToken)
                               .ConfigureAwait(false);
                            if (!readAllowed)
                            {
                                break;
                            }

                            var command = await ftpCommandReader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false);
                            await ProcessAsync(command, ftpResponseWriter, serverCommandWriter)
                               .ConfigureAwait(false);
                        }
                    }
                }

                await serverStatusListenerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore the OperationCanceledException
                // This is normal during disconnects
            }
            catch (Exception ex)
            {
                Log?.LogError(ex, "Failed to read command. Connection closed?");
            }
            finally
            {
                _state.Features.Set<ITlsConnectionFeature>(null);
            }
        }

        private async Task ProcessAsync(
            FtpCommand command,
            ChannelWriter<IFtpResponse> responseWriter,
            ChannelWriter<FtpServerCommand> serverCommandWriter)
        {
            var context = new FtpContext(command, _state, responseWriter, serverCommandWriter);
            await _requestDelegate(context);
        }

        private async Task StartServerStatusListener(
            ChannelReader<FtpServerStatus> serverStatusReader,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var status = await serverStatusReader.ReadAsync(cancellationToken)
                   .ConfigureAwait(false);
                do
                {
                    _serverStatusEventFeature.OnStatus(status);
                }
                while (serverStatusReader.TryRead(out status));
            }
        }

        private async Task DispatchCommandAsync(FtpContext context)
        {
            Log?.Trace(context.Command);

            var cancellationToken = context.State.Features.Get<IFtpConnectionLifetimeFeature>().ConnectionClosed;

            var commandHandler = FindCommandHandler(context.Command);
            if (commandHandler == null)
            {
                await context.ResponseWriter
                   .WriteAsync(new FtpResponse(500, T("Syntax error, command unrecognized.")), cancellationToken)
                   .ConfigureAwait(false);
                return;
            }

            var handler = commandHandler.Item2;
            var handlerCommand = commandHandler.Item1;
            var isLoginRequired = commandHandler.Item3;
            if (isLoginRequired && _loginStateMachine.Status != SecurityStatus.Authorized)
            {
                await context.ResponseWriter.WriteAsync(
                        new FtpResponse(530, T("Not logged in.")),
                        cancellationToken)
                   .ConfigureAwait(false);
                return;
            }

            IFtpResponse response = null;
            _ftpContextAccessor.FtpContext = context;
            try
            {
                var cmdHandler = handler as FtpCommandHandler;
                var isAbortable = cmdHandler?.IsAbortable ?? false;
                if (isAbortable)
                {
                    if (_activeBackgroundCommandTask != null)
                    {
                        await context.ResponseWriter.WriteAsync(
                                new FtpResponse(503, T("Parallel commands aren't allowed.")),
                                cancellationToken)
                           .ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        // Cancellation token only for this request
                        var backgroundCts = new CancellationTokenSource();

                        // Attach to cancellation token for connection
                        _activeBackgroundCommandCtRegistration =
                            cancellationToken.Register(() => backgroundCts.Cancel(true));

                        _activeBackgroundCommandTask = handler.Process(handlerCommand, backgroundCts.Token);
                    }
                }
                else
                {
                    response = await handler.Process(handlerCommand, cancellationToken)
                       .ConfigureAwait(false);
                }
            }
            catch (FtpDataConnectionException dce)
            {
                Log?.LogInformation($"Opening data connection ({context.Command}) failed with error {dce.Message}");
                response = new FtpResponse(425, dce.Message);
            }
            catch (FileSystemException fse)
            {
                var message = fse.Message != null ? $"{fse.FtpErrorName}: {fse.Message}" : fse.FtpErrorName;
                Log?.LogInformation($"Rejected command ({context.Command}) with error {fse.FtpErrorCode} {message}");
                response = new FtpResponse(fse.FtpErrorCode, message);
            }
            catch (NotSupportedException nse)
            {
                var message = nse.Message ?? T("Command {0} not supported", context.Command);
                Log?.LogInformation(message);
                response = new FtpResponse(502, message);
            }
            catch (Exception ex)
            {
                Log?.LogError(ex, "Failed to process message ({0})", context.Command);
                response = new FtpResponse(501, T("Syntax error in parameters or arguments."));
            }
            finally
            {
                _ftpContextAccessor.FtpContext = null;
            }

            if (response != null)
            {
                await context.ResponseWriter.WriteAsync(response, cancellationToken)
                   .ConfigureAwait(false);

                if (response.Code == 421)
                {
                    await context.ServerCommandWriter.WriteAsync(FtpServerCommand.Shutdown, cancellationToken)
                       .ConfigureAwait(false);
                    context.ResponseWriter.Complete();
                }
            }
        }

        private Tuple<FtpCommand, IFtpCommandBase, bool> FindCommandHandler(FtpCommand command)
        {
            if (!CommandHandlers.TryGetValue(command.Name, out var handler))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(command.Argument) && handler is IFtpCommandHandlerExtensionHost extensionHost)
            {
                var extensionCommand = FtpCommand.Parse(command.Argument);
                if (extensionHost.Extensions.TryGetValue(extensionCommand.Name, out var extension))
                {
                    return Tuple.Create(extensionCommand, (IFtpCommandBase)extension, extension.IsLoginRequired ?? handler.IsLoginRequired);
                }
            }

            return Tuple.Create(command, (IFtpCommandBase)handler, handler.IsLoginRequired);
        }

        /// <summary>
        /// Translates a message using the current catalog of the active connection.
        /// </summary>
        /// <param name="message">The message to translate.</param>
        /// <returns>The translated message.</returns>
        private string T(string message)
        {
            return _state.Catalog.GetString(message);
        }

        /// <summary>
        /// Translates a message using the current catalog of the active connection.
        /// </summary>
        /// <param name="message">The message to translate.</param>
        /// <param name="args">The format arguments.</param>
        /// <returns>The translated message.</returns>
        [StringFormatMethod("message")]
        private string T(string message, params object[] args)
        {
            return _state.Catalog.GetString(message, args);
        }

        private class ServerStatusEventFeature : IServerStatusEventFeature
        {
            private readonly FtpCommandMultiplexer _source;

            public ServerStatusEventFeature(FtpCommandMultiplexer source)
            {
                _source = source;
            }

            /// <inheritdoc />
            public event EventHandler<FtpServerStatusEventArgs> Status;

            /// <inheritdoc />
            public void OnStatus(FtpServerStatus status)
            {
                Status?.Invoke(_source, new FtpServerStatusEventArgs(status));
            }
        }
    }
}
