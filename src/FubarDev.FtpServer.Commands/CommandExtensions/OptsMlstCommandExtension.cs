// <copyright file="OptsMlstCommandExtension.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.CommandHandlers;
using FubarDev.FtpServer.Features;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandExtensions
{
    /// <summary>
    /// <c>MLST</c> extension for the <c>OPTS</c> command.
    /// </summary>
    public class OptsMlstCommandExtension : FtpCommandHandlerExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptsMlstCommandExtension"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public OptsMlstCommandExtension(
            [NotNull] IFtpContextAccessor ftpContextAccessor)
            : base(ftpContextAccessor, "OPTS", "MLST")
        {
            // Don't announce this extension, because it gets already announced
            // by the MLST command itself.
            AnnouncementMode = ExtensionAnnouncementMode.Hidden;
        }

        /// <inheritdoc />
        public override void InitializeConnectionData()
        {
            IMlstFactsFeature feature = new MlstFactsFeature();
            foreach (var knownFact in MlstCommandHandler.KnownFacts)
            {
                feature.ActivaFacts.Add(knownFact);
            }

            FtpContext.State.Features.Set(feature);
        }

        /// <inheritdoc />
        public override Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var feature = FtpContext.State.Features.Get<IMlstFactsFeature>();
            var facts = command.Argument.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            feature.ActivaFacts.Clear();
            foreach (var fact in facts)
            {
                if (!MlstCommandHandler.KnownFacts.Contains(fact))
                {
                    return Task.FromResult<IFtpResponse>(new FtpResponse(501, T("Syntax error in parameters or arguments.")));
                }

                feature.ActivaFacts.Add(fact);
            }

            return Task.FromResult<IFtpResponse>(new FtpResponse(200, T("Command okay.")));
        }
    }
}
