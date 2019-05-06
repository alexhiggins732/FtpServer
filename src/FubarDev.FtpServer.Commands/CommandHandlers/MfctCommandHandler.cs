//-----------------------------------------------------------------------
// <copyright file="MfctCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.ListFormatters.Facts;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>MFCT</c> command.
    /// </summary>
    public class MfctCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MfctCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public MfctCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor)
            : base(ftpContextAccessor, "MFCT")
        {
        }

        /// <inheritdoc/>
        public override IEnumerable<IFeatureInfo> GetSupportedFeatures()
        {
            yield return new GenericFeatureInfo("MFCT", IsLoginRequired);
        }

        /// <inheritdoc/>
        public override async Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var parts = command.Argument.Split(new[] { ' ' }, 2);
            if (parts.Length != 2)
            {
                return new FtpResponse(551, T("Timestamp or file name missing."));
            }

            if (!parts[0].TryParseTimestamp("UTC", out var createTime))
            {
                return new FtpResponse(551, T("Invalid timestamp."));
            }

            var path = parts[1];
            var currentPath = Data.Path.Clone();
            var fileInfo = await Data.FileSystem.SearchFileAsync(currentPath, path, cancellationToken).ConfigureAwait(false);
            if (fileInfo?.Entry == null)
            {
                return new FtpResponse(550, T("File not found."));
            }

            await Data.FileSystem.SetMacTimeAsync(fileInfo.Entry, null, null, createTime, cancellationToken).ConfigureAwait(false);

            var fact = new CreateFact(createTime);
            var fullName = currentPath.GetFullPath() + fileInfo.FileName;

            return new FtpResponse(213, T("{0}={1}; {2}", fact.Name, fact.Value, fullName));
        }
    }
}
