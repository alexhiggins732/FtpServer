//-----------------------------------------------------------------------
// <copyright file="AppeCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.Authentication;
using FubarDev.FtpServer.BackgroundTransfer;
using FubarDev.FtpServer.FileSystem;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>APPE</c> command.
    /// </summary>
    public class AppeCommandHandler : FtpCommandHandler
    {
        [NotNull]
        private readonly IBackgroundTransferWorker _backgroundTransferWorker;

        [NotNull]
        private readonly ISslStreamWrapperFactory _sslStreamWrapperFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppeCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        /// <param name="backgroundTransferWorker">The background transfer worker service.</param>
        /// <param name="sslStreamWrapperFactory">An object to handle SSL streams.</param>
        public AppeCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor,
            [NotNull] IBackgroundTransferWorker backgroundTransferWorker,
            [NotNull] ISslStreamWrapperFactory sslStreamWrapperFactory)
            : base(ftpContextAccessor, "APPE")
        {
            _backgroundTransferWorker = backgroundTransferWorker;
            _sslStreamWrapperFactory = sslStreamWrapperFactory;
        }

        /// <inheritdoc/>
        public override bool IsAbortable => true;

        /// <inheritdoc/>
        public override async Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var restartPosition = Data.RestartPosition;
            Data.RestartPosition = null;

            if (!Data.TransferMode.IsBinary && Data.TransferMode.FileType != FtpFileType.Ascii)
            {
                throw new NotSupportedException();
            }

            var fileName = command.Argument;
            if (string.IsNullOrEmpty(fileName))
            {
                return new FtpResponse(501, T("No file name specified"));
            }

            var currentPath = Data.Path.Clone();
            var fileInfo = await Data.FileSystem.SearchFileAsync(currentPath, fileName, cancellationToken).ConfigureAwait(false);
            if (fileInfo == null)
            {
                return new FtpResponse(550, T("Not a valid directory."));
            }

            if (fileInfo.FileName == null)
            {
                return new FtpResponse(553, T("File name not allowed."));
            }

            await Connection.WriteAsync(new FtpResponse(150, T("Opening connection for data transfer.")), cancellationToken).ConfigureAwait(false);

            return await Connection
                .SendResponseAsync(client => ExecuteSend(client, fileInfo, restartPosition, cancellationToken))
                .ConfigureAwait(false);
        }

        private async Task<IFtpResponse> ExecuteSend(
            TcpClient responseSocket,
            [NotNull] SearchResult<IUnixFileEntry> fileInfo,
            long? restartPosition,
            CancellationToken cancellationToken)
        {
            var responseStream = responseSocket.GetStream();
            responseStream.ReadTimeout = 10000;

            using (var stream = await Connection.CreateEncryptedStream(responseStream).ConfigureAwait(false))
            {
                IBackgroundTransfer backgroundTransfer;
                if ((restartPosition != null && restartPosition.Value == 0) || fileInfo.Entry == null)
                {
                    if (fileInfo.Entry == null)
                    {
                        backgroundTransfer = await Data.FileSystem
                            .CreateAsync(
                                fileInfo.Directory,
                                fileInfo.FileName ?? throw new InvalidOperationException(),
                                stream,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        backgroundTransfer = await Data.FileSystem
                            .ReplaceAsync(fileInfo.Entry, stream, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    backgroundTransfer = await Data.FileSystem
                        .AppendAsync(fileInfo.Entry, restartPosition, stream, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (backgroundTransfer != null)
                {
                    _backgroundTransferWorker.Enqueue(backgroundTransfer);
                }

                await _sslStreamWrapperFactory.CloseStreamAsync(stream, cancellationToken)
                   .ConfigureAwait(false);
            }

            return new FtpResponse(226, T("Uploaded file successfully."));
        }
    }
}
