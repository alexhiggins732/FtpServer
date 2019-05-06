// <copyright file="IPasvAddressResolver.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Net;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace FubarDev.FtpServer
{
    /// <summary>
    /// Interface to get the options for the <c>PASV</c>/<c>EPSV</c> commands.
    /// </summary>
    public interface IPasvAddressResolver
    {
        /// <summary>
        /// Get the <c>PASV</c>/<c>EPSV</c> options.
        /// </summary>
        /// <param name="connection">The FTP connection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task returning the options.</returns>
        [NotNull]
        [ItemNotNull]
        Task<PasvListenerOptions> GetOptionsAsync(
            [NotNull] IPAddress localAddress,
            CancellationToken cancellationToken);
    }
}
