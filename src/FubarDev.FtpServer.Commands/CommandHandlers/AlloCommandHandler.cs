//-----------------------------------------------------------------------
// <copyright file="AlloCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>ALLO</c> command.
    /// </summary>
    public class AlloCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlloCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public AlloCommandHandler([NotNull] IFtpContextAccessor ftpContextAccessor)
            : base(ftpContextAccessor, "ALLO")
        {
        }

        /// <inheritdoc/>
        public override bool IsLoginRequired => false;

        /// <inheritdoc/>
        public override Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult<IFtpResponse>(new FtpResponse(202, T("Allo processed successfully (deprecated).")));
        }
    }
}
