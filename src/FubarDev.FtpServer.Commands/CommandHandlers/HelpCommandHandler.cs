// <copyright file="HelpCommandHandler.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// The <c>HELP</c> command handler.
    /// </summary>
    public class HelpCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public HelpCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor)
            : base(ftpContextAccessor, "HELP")
        {
        }

        /// <inheritdoc/>
        public override bool IsLoginRequired => false;

        /// <inheritdoc/>
        public override Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var helpArg = command.Argument;
            if (string.IsNullOrEmpty(helpArg))
            {
                helpArg = "SITE";
            }

            switch (helpArg)
            {
                case "SITE":
                    return ShowHelpSiteAsync();
                default:
                    return Task.FromResult<IFtpResponse>(new FtpResponse(501, T("Syntax error in parameters or arguments.")));
            }
        }

        private Task<IFtpResponse> ShowHelpSiteAsync()
        {
            var helpText = new[]
            {
                "SITE BLST [DIRECT]",
            };

            return Task.FromResult<IFtpResponse>(
                new FtpResponseList(
                    211,
                    "HELP",
                    "HELP",
                    helpText));
        }
    }
}
