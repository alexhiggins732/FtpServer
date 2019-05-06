//-----------------------------------------------------------------------
// <copyright file="SystCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>SYST</c> command.
    /// </summary>
    public class SystCommandHandler : FtpCommandHandler
    {
        private readonly string _operatingSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        /// <param name="options">Options for the SYST command.</param>
        public SystCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor,
            [NotNull] IOptions<SystCommandOptions> options)
            : base(ftpContextAccessor, "SYST")
        {
            _operatingSystem = options.Value.OperatingSystem ?? "UNIX";
        }

        /// <inheritdoc/>
        public override Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult<IFtpResponse>(new FtpResponse(215, T("{0} Type: {1}", _operatingSystem, Connection.Data.TransferMode)));
        }
    }
}
