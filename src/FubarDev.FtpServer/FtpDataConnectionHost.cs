// <copyright file="FtpDataConnectionHost.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using FubarDev.FtpServer.Features;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace FubarDev.FtpServer
{
    public class FtpDataConnectionHost : IFtpDataConnectionHost
    {
        [NotNull]
        private readonly IPasvListenerFactory _pasvListenerFactory;

        [NotNull]
        private readonly IConnectionFeature _connectionFeature;

        [NotNull]
        private readonly ActiveConnectionInformation _defaultActiveInfo;

        [CanBeNull]
        private readonly ILogger _logger;

        [NotNull]
        private ActiveConnectionInformation _activeInfo;

        [CanBeNull]
        private PassiveConnectionInformation _passiveInfo;

        public FtpDataConnectionHost(
            [NotNull] IPasvListenerFactory pasvListenerFactory,
            [NotNull] IConnectionFeature connectionFeature)
        {
            _pasvListenerFactory = pasvListenerFactory;
            _connectionFeature = connectionFeature;
            _logger = connectionFeature.Logger;

            Debug.Assert(_connectionFeature.RemoteAddress.IPAddress != null, "_connectionFeature.RemoteAddress.IPAddress != null");

            // Initialize with default values
            _activeInfo = _defaultActiveInfo = new ActiveConnectionInformation(
                _connectionFeature.RemoteAddress.IPAddress,
                _connectionFeature.RemoteAddress.Port,
                null);
        }

        /// <inheritdoc />
        public FtpDataConnectionMode? SelectedDataConnectionMode { get; set; }

        /// <inheritdoc />
        public Task UseActiveDataConnectionAsync(Address address = null, CancellationToken cancellationToken = default)
        {
            Debug.Assert(_connectionFeature.RemoteAddress.IPAddress != null, "_connectionFeature.RemoteAddress.IPAddress != null");

            _activeInfo = new ActiveConnectionInformation(
                _connectionFeature.RemoteAddress.IPAddress,
                _connectionFeature.RemoteAddress.Port,
                address);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<Address> UsePassiveDataConnectionAsync(int? port = null, CancellationToken cancellationToken = default)
        {
            Debug.Assert(_connectionFeature.LocalAddress.IPAddress != null, "_connectionFeature.LocalAddress.IPAddress != null");

            var listener = await _pasvListenerFactory.CreateTcpListenerAsync(
                    _connectionFeature.LocalAddress.IPAddress,
                    port ?? 0,
                    cancellationToken)
               .ConfigureAwait(false);

            var cts = new CancellationTokenSource();
            var tcpClientChannel = Channel.CreateUnbounded<TcpClient>();
            var listenerTask = StartPassiveDataConnectionListenerAsync(tcpClientChannel, listener, cts.Token);
            _passiveInfo = new PassiveConnectionInformation(cts, listenerTask, listener, tcpClientChannel);

            return new Address(listener.PasvEndPoint.Address.ToString(), listener.PasvEndPoint.Port);
        }

        /// <inheritdoc />
        public Task<TcpClient> OpenDataConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (_passiveInfo != null)
            {
                return OpenPassiveDataConnectionAsync(_passiveInfo.TcpClientReader, timeout, cancellationToken);
            }

            return OpenActiveDataConnectionAsync(timeout, cancellationToken);
        }

        /// <inheritdoc />
        public void Reset()
        {
            try
            {
                _passiveInfo?.ListenerCts.Cancel();
                _passiveInfo?.Listener.Dispose();
            }
            finally
            {
                _passiveInfo = null;
                _activeInfo = _defaultActiveInfo;
                SelectedDataConnectionMode = null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Reset();
        }

        private async Task StartPassiveDataConnectionListenerAsync(
            [NotNull] ChannelWriter<TcpClient> acceptedClients,
            IPasvListener listener,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var acceptTask = listener.AcceptPasvClientAsync();

                var resultTask = await Task.WhenAny(acceptTask, Task.Delay(-1, cancellationToken))
                   .ConfigureAwait(false);

                // delay was cancelled
                if (resultTask != acceptTask)
                {
                    break;
                }

                var passiveClient = acceptTask.Result;
                if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
                {
                    var pasvRemoteAddress = ((IPEndPoint)passiveClient.Client.RemoteEndPoint).Address;
                    _logger?.LogDebug($"Client connected from {pasvRemoteAddress} for a passive data connection.");
                }

                await acceptedClients.WriteAsync(passiveClient, cancellationToken)
                   .ConfigureAwait(false);
            }
        }

        private async Task<TcpClient> OpenPassiveDataConnectionAsync(ChannelReader<TcpClient> tcpClientReader, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var readerTask = tcpClientReader.ReadAsync(cancellationToken).AsTask();
            var resultTask = await Task.WhenAny(readerTask, Task.Delay(timeout, cancellationToken))
               .ConfigureAwait(false);
            if (resultTask == readerTask)
            {
                return readerTask.Result;
            }

            throw new TimeoutException();
        }

        private async Task<TcpClient> OpenActiveDataConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var result = new TcpClient(_activeInfo.ClientAddress.AddressFamily);
            var connectTask = result.ConnectAsync(_activeInfo.ClientAddress, _activeInfo.ClientPort);
            var resultTask = await Task.WhenAny(connectTask, Task.Delay(timeout, cancellationToken));
            if (resultTask != connectTask)
            {
                throw new TimeoutException();
            }

            try
            {
                await connectTask.ConfigureAwait(false);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                throw new TimeoutException();
            }

            return result;
        }

        private class ActiveConnectionInformation
        {
            public ActiveConnectionInformation(
                [NotNull] IPAddress remoteAddress,
                int remotePort,
                [CanBeNull] Address activeModeAddress)
            {
                ClientAddress = activeModeAddress?.IPAddress ?? remoteAddress;
                ClientPort = activeModeAddress?.Port ?? remotePort;
            }

            [NotNull]
            public IPAddress ClientAddress { get; }

            public int ClientPort { get; }
        }

        private class PassiveConnectionInformation
        {
            [NotNull]
            [UsedImplicitly]
#pragma warning disable IDE0052 // Is used as anchor to avoid disposal by the GC
            private readonly Task _listenerTask;
#pragma warning restore IDE0052

            public PassiveConnectionInformation(
                [NotNull] CancellationTokenSource listenerCts,
                [NotNull] Task listenerTask,
                [NotNull] IPasvListener listener,
                [NotNull] ChannelReader<TcpClient> tcpClientReader)
            {
                ListenerCts = listenerCts;
                Listener = listener;
                TcpClientReader = tcpClientReader;
                _listenerTask = listenerTask;
            }

            [NotNull]
            public CancellationTokenSource ListenerCts { get; }

            [NotNull]
            public IPasvListener Listener { get; }

            [NotNull]
            public ChannelReader<TcpClient> TcpClientReader { get; }
        }
    }
}
