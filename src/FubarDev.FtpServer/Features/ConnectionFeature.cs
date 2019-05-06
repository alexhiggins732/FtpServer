using System.Net;
using System.Net.Sockets;
using System.Text;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FubarDev.FtpServer.Features
{
    public class ConnectionFeature : IConnectionFeature
    {
        public ConnectionFeature(
            [NotNull] TcpClient socket,
            [NotNull] IOptions<FtpConnectionOptions> options,
            [CanBeNull] ILogger<FtpClientHandler> clientLogger = null)
        {
            var remoteEndPoint = (IPEndPoint)socket.Client.RemoteEndPoint;
            var localEndPoint = (IPEndPoint)socket.Client.LocalEndPoint;
            RemoteAddress = new Address(remoteEndPoint.Address.ToString(), remoteEndPoint.Port);
            LocalAddress = new Address(localEndPoint.Address.ToString(), localEndPoint.Port);
            Encoding = options.Value.DefaultEncoding ?? Encoding.ASCII;
            Logger = clientLogger;
        }

        public Address LocalAddress { get; }

        public Address RemoteAddress { get; }

        public Encoding Encoding { get; set; }

        /// <inheritdoc />
        public ILogger Logger { get; }
    }
}
