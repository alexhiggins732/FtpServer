// <copyright file="ILocalizationFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Globalization;

using FubarDev.FtpServer.Localization;

using JetBrains.Annotations;

using NGettext;

namespace FubarDev.FtpServer.Features
{
    public interface ILocalizationFeature
    {
        [NotNull]
        IFtpCatalogLoader CatalogLoader { get; }

        /// <summary>
        /// Gets or sets the catalog to be used by the default FTP server implementation.
        /// </summary>
        [NotNull]
        ICatalog Catalog { get; set; }

        /// <summary>
        /// Gets or sets the selected language.
        /// </summary>
        [NotNull]
        CultureInfo Language { get; set; }
    }
}
