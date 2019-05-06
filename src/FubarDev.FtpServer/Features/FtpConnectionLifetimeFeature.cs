// <copyright file="FtpConnectionLifetimeFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;

namespace FubarDev.FtpServer.Features
{
    public class FtpConnectionLifetimeFeature : IFtpConnectionLifetimeFeature
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public FtpConnectionLifetimeFeature(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
            ConnectionClosed = cancellationTokenSource.Token;
        }

        /// <inheritdoc />
        public CancellationToken ConnectionClosed { get; set; }

        /// <inheritdoc />
        public void Abort()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
