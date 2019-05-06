//-----------------------------------------------------------------------
// <copyright file="PwdCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.FileSystem;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>PWD</c> command.
    /// </summary>
    public class PwdCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PwdCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public PwdCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor)
            : base(ftpContextAccessor, "PWD", "XPWD")
        {
        }

        /// <inheritdoc/>
        public override Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var path = Connection.Data.Path.GetFullPath();
            if (path.EndsWith("/") && path.Length > 1)
            {
                path = path.Substring(0, path.Length - 1);
            }

            return Task.FromResult<IFtpResponse>(new FtpResponse(257, T("\"{0}\"", path)));
        }
    }
}
