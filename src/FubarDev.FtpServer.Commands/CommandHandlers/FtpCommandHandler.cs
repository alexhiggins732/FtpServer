//-----------------------------------------------------------------------
// <copyright file="FtpCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// The base class for all FTP command handlers.
    /// </summary>
    public abstract class FtpCommandHandler : IFtpCommandHandler
    {
        private readonly IFtpContextAccessor _ftpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpCommandHandler"/> class.
        /// </summary>
        /// <param name="ftpContextAccessor">The accessor to get the context that is active during the <see cref="Process"/> method execution.</param>
        /// <param name="name">The command name.</param>
        /// <param name="alternativeNames">Alternative names.</param>
        protected FtpCommandHandler([NotNull] IFtpContextAccessor ftpContextAccessor, [NotNull] string name, [NotNull, ItemNotNull] params string[] alternativeNames)
        {
            _ftpContextAccessor = ftpContextAccessor;
            var names = new List<string>
            {
                name,
            };
            names.AddRange(alternativeNames);
            Names = names;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> Names { get; }

        /// <inheritdoc />
        public FtpContext FtpContext
            => _ftpContextAccessor.FtpContext
               ?? throw new InvalidOperationException(
                   "The FTP context was used outside of an active connection.");

        /// <inheritdoc />
        public virtual bool IsLoginRequired => true;

        /// <inheritdoc />
        public virtual bool IsAbortable => false;

        /// <summary>
        /// Gets the connection data.
        /// </summary>
        [NotNull]
        protected FtpConnectionData Data => Connection.Data;

        /// <inheritdoc />
        public virtual IEnumerable<IFeatureInfo> GetSupportedFeatures()
        {
            return Enumerable.Empty<IFeatureInfo>();
        }

        /// <inheritdoc />
        public virtual IEnumerable<IFtpCommandHandlerExtension> GetExtensions()
        {
            return Enumerable.Empty<IFtpCommandHandlerExtension>();
        }

        /// <inheritdoc />
        public abstract Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Translates a message using the current catalog of the active connection.
        /// </summary>
        /// <param name="message">The message to translate.</param>
        /// <returns>The translated message.</returns>
        protected string T(string message)
        {
            return FtpContext.State.Catalog.GetString(message);
        }

        /// <summary>
        /// Translates a message using the current catalog of the active connection.
        /// </summary>
        /// <param name="message">The message to translate.</param>
        /// <param name="args">The format arguments.</param>
        /// <returns>The translated message.</returns>
        [StringFormatMethod("message")]
        protected string T(string message, params object[] args)
        {
            return FtpContext.State.Catalog.GetString(message, args);
        }
    }
}
