// <copyright file="FtpCommandHandlerExtension.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandExtensions
{
    /// <summary>
    /// The base class for FTP command extensions.
    /// </summary>
    public abstract class FtpCommandHandlerExtension : IFtpCommandHandlerExtension
    {
        [NotNull]
        private readonly IFtpContextAccessor _ftpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpCommandHandlerExtension"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        /// <param name="extensionFor">The name of the command this extension is for.</param>
        /// <param name="name">The command name.</param>
        /// <param name="alternativeNames">Alternative names.</param>
        protected FtpCommandHandlerExtension(
            [NotNull] IFtpContextAccessor ftpContextAccessor,
            [NotNull] string extensionFor,
            [NotNull] string name,
            [NotNull, ItemNotNull] params string[] alternativeNames)
        {
            _ftpContextAccessor = ftpContextAccessor;
            var names = new List<string>
            {
                name,
            };
            names.AddRange(alternativeNames);
            Names = names;
            ExtensionFor = extensionFor;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> Names { get; }

        /// <inheritdoc />
        public FtpContext FtpContext
            => _ftpContextAccessor.FtpContext
               ?? throw new InvalidOperationException("The connection information was used outside of an active connection.");

        /// <inheritdoc />
        public virtual bool? IsLoginRequired { get; set; }

        /// <inheritdoc />
        public string ExtensionFor { get; }

        /// <summary>
        /// Gets or sets the extension announcement mode.
        /// </summary>
        public ExtensionAnnouncementMode AnnouncementMode { get; set; } = ExtensionAnnouncementMode.Hidden;

        /// <inheritdoc />
        public abstract void InitializeConnectionData();

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
