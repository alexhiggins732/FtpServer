// <copyright file="LangCommandHandler.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.Localization;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>LANG</c> command.
    /// </summary>
    public class LangCommandHandler : FtpCommandHandler
    {
        private static readonly Regex _languagePattern = new Regex(
            "^[a-z]{1,8}(-[A-Z]{1,8})*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [NotNull]
        private readonly IFtpCatalogLoader _catalogLoader;

        /// <summary>
        /// Initializes a new instance of the <see cref="LangCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        /// <param name="catalogLoader">The catalog loader for the FTP server.</param>
        public LangCommandHandler(
            [NotNull] IFtpContextAccessor ftpContextAccessor,
            [NotNull] IFtpCatalogLoader catalogLoader)
            : base(ftpContextAccessor, "LANG")
        {
            _catalogLoader = catalogLoader;
        }

        /// <inheritdoc />
        public override IEnumerable<IFeatureInfo> GetSupportedFeatures()
        {
            yield return new GenericFeatureInfo(
                "FEAT",
                connection =>
                {
#if NETSTANDARD1_3
                    var currentLanguage = connection.Data.Language.Name;
#else
                    var currentLanguage = connection.Data.Language.IetfLanguageTag;
#endif
                    var languages = _catalogLoader.GetSupportedLanguages()
                        .Select(x => x + (string.Equals(x, currentLanguage) ? "*" : string.Empty));
                    return "LANG " + string.Join(";", languages);
                },
                false);
        }

        /// <inheritdoc />
        public override async Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.Argument))
            {
                Connection.Data.Language = _catalogLoader.DefaultLanguage;
                Connection.Data.Catalog = _catalogLoader.DefaultCatalog;
            }
            else
            {
                var match = _languagePattern.Match(command.Argument.Trim());
                if (!match.Success)
                {
                    return new FtpResponse(501, T("Bad argument"));
                }

                try
                {
                    var language = new CultureInfo(match.Value);
                    var catalog = await _catalogLoader.LoadAsync(language, cancellationToken)
                        .ConfigureAwait(false);
                    if (catalog is null)
                    {
                        return new FtpResponse(504, T("Unsupported parameter"));
                    }

                    Connection.Data.Language = language;
                    Connection.Data.Catalog = catalog;
                }
                catch (CultureNotFoundException)
                {
                    return new FtpResponse(504, T("Unsupported parameter"));
                }
            }

            return new FtpResponse(200, T("Command okay"));
        }
    }
}
