//-----------------------------------------------------------------------
// <copyright file="SizeCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.FileSystem;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>SIZE</c> command.
    /// </summary>
    public class SizeCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SizeCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public SizeCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor)
            : base(ftpContextAccessor, "SIZE")
        {
        }

        /// <inheritdoc/>
        public override IEnumerable<IFeatureInfo> GetSupportedFeatures()
        {
            yield return new GenericFeatureInfo("SIZE", IsLoginRequired);
        }

        /// <inheritdoc/>
        public override async Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var fileName = command.Argument;
            var tempPath = Data.Path.Clone();
            var fileInfo = await Data.FileSystem.SearchFileAsync(tempPath, fileName, cancellationToken).ConfigureAwait(false);
            if (fileInfo?.Entry == null)
            {
                return new FtpResponse(550, T("File not found ({0}).", fileName));
            }

            return new FtpResponse(213, T("{0}", fileInfo.Entry.Size));
        }
    }
}
