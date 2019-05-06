//-----------------------------------------------------------------------
// <copyright file="RmdCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.FileSystem;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>RMD</c> command.
    /// </summary>
    public class RmdCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RmdCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public RmdCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor)
            : base(ftpContextAccessor, "RMD")
        {
        }

        /// <inheritdoc/>
        public override async Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var path = command.Argument;
            var currentPath = Data.Path.Clone();
            var subDir = await Data.FileSystem.GetDirectoryAsync(currentPath, path, cancellationToken).ConfigureAwait(false);
            if (subDir == null)
            {
                return new FtpResponse(550, T("Not a valid directory."));
            }

            try
            {
                if (Data.Path.IsChildOfOrSameAs(currentPath, Data.FileSystem))
                {
                    return new FtpResponse(550, T("Not a valid directory (is same or parent of current directory)."));
                }

                await Data.FileSystem.UnlinkAsync(subDir, cancellationToken).ConfigureAwait(false);
                return new FtpResponse(250, T("Directory removed."));
            }
            catch (Exception)
            {
                return new FtpResponse(550, T("Couldn't remove directory (locked?)."));
            }
        }
    }
}
