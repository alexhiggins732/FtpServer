// <copyright file="TlsConnectionFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.Features
{
    public class TlsConnectionFeature : ITlsConnectionFeature
    {
        [NotNull]
        private readonly ChannelWriter<FtpServerCommand> _serverCommandWriter;

        [NotNull]
        private readonly IServerStatusEventFeature _serverStatusEventFeature;

        public TlsConnectionFeature(
            [NotNull] ChannelWriter<FtpServerCommand> serverCommandWriter,
            [NotNull] IServerStatusEventFeature serverStatusEventFeature)
        {
            _serverCommandWriter = serverCommandWriter;
            _serverStatusEventFeature = serverStatusEventFeature;
        }

        /// <inheritdoc />
        public bool IsTlsEnabled { get; private set; }

        /// <inheritdoc />
        public async Task<FtpServerStatus> EnableTlsAsync(CancellationToken cancellationToken)
        {
            var responseAvailable = new SemaphoreSlim(0, 1);
            FtpServerStatus? status = null;
            void StatusReceived(object sender, FtpServerStatusEventArgs e)
            {
                switch (e.Status)
                {
                    case FtpServerStatus.TlsEnabled:
                    case FtpServerStatus.TlsEnableError:
                    case FtpServerStatus.TlsEnableErrorNotConfigured:
                        status = e.Status;
                        IsTlsEnabled = e.Status == FtpServerStatus.TlsEnabled;
                        responseAvailable.Release();
                        break;
                }
            }

            _serverStatusEventFeature.Status += StatusReceived;
            try
            {
                await _serverCommandWriter.WriteAsync(
                        FtpServerCommand.EnableTls,
                        cancellationToken)
                   .ConfigureAwait(false);

                await responseAvailable.WaitAsync(cancellationToken)
                   .ConfigureAwait(false);
            }
            finally
            {
                _serverStatusEventFeature.Status -= StatusReceived;
            }

            return status ?? throw new InvalidOperationException("No status received");
        }

        /// <inheritdoc />
        public async Task<FtpServerStatus> DisableTlsAsync(CancellationToken cancellationToken)
        {
            var responseAvailable = new SemaphoreSlim(0, 1);
            FtpServerStatus? status = null;
            void StatusReceived(object sender, FtpServerStatusEventArgs e)
            {
                switch (e.Status)
                {
                    case FtpServerStatus.TlsDisabled:
                    case FtpServerStatus.TlsWasDisabled:
                        status = e.Status;
                        IsTlsEnabled = false;
                        responseAvailable.Release();
                        break;
                }
            }

            _serverStatusEventFeature.Status += StatusReceived;
            try
            {
                await _serverCommandWriter.WriteAsync(
                        FtpServerCommand.EnableTls,
                        cancellationToken)
                   .ConfigureAwait(false);

                await responseAvailable.WaitAsync(cancellationToken)
                   .ConfigureAwait(false);
            }
            finally
            {
                _serverStatusEventFeature.Status -= StatusReceived;
            }

            return status ?? throw new InvalidOperationException("No status received");
        }
    }
}
