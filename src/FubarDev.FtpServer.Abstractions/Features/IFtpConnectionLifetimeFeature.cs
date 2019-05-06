// <copyright file="IFtpConnectionLifetimeFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;

namespace FubarDev.FtpServer.Features
{
    public interface IFtpConnectionLifetimeFeature
    {
        CancellationToken ConnectionClosed { get; set; }

        void Abort();
    }
}
