// <copyright file="IFtpDataConnectionHost.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.FtpServer
{
    public interface IFtpDataConnectionHost : IDisposable
    {
        FtpDataConnectionMode? SelectedDataConnectionMode { get; set; }

        void Reset();

        Task UseActiveDataConnectionAsync(Address address = null, CancellationToken cancellationToken = default);

        Task<Address> UsePassiveDataConnectionAsync(int? port = null, CancellationToken cancellationToken = default);

        Task<TcpClient> OpenDataConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
