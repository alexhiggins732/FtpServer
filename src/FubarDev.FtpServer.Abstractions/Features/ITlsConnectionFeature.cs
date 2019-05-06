// <copyright file="ITlsConnectionFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.FtpServer.Features
{
    public interface ITlsConnectionFeature
    {
        bool IsTlsEnabled { get; }

        Task<FtpServerStatus> EnableTlsAsync(CancellationToken cancellationToken);

        Task<FtpServerStatus> DisableTlsAsync(CancellationToken cancellationToken);
    }
}
