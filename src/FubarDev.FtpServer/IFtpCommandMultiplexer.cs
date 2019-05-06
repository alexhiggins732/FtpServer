// <copyright file="IFtpCommandMultiplexer.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FubarDev.FtpServer
{
    public interface IFtpCommandMultiplexer
    {
        Task ExecuteAsync(
            ChannelWriter<FtpServerCommand> serverCommandWriter,
            ChannelReader<FtpServerStatus> serverStatusReader,
            ChannelReader<FtpCommand> ftpCommandReader,
            ChannelWriter<IFtpResponse> ftpResponseWriter,
            CancellationToken cancellationToken);
    }
}
