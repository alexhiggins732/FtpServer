// <copyright file="FtpContext.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading.Channels;

using JetBrains.Annotations;

namespace FubarDev.FtpServer
{
    public class FtpContext
    {
        public FtpContext(
            [NotNull] FtpCommand command,
            [NotNull] IFtpConnectionState state,
            [NotNull] ChannelWriter<IFtpResponse> responseWriter,
            [NotNull] ChannelWriter<FtpServerCommand> serverCommandWriter)
        {
            Command = command;
            State = state;
            ResponseWriter = responseWriter;
            ServerCommandWriter = serverCommandWriter;
        }

        [NotNull]
        public FtpCommand Command { get; }

        [NotNull]
        public IFtpConnectionState State { get; }

        [NotNull]
        public ChannelWriter<IFtpResponse> ResponseWriter { get; }

        [NotNull]
        public ChannelWriter<FtpServerCommand> ServerCommandWriter { get; }
    }
}
