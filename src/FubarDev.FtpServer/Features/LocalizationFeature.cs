// <copyright file="LocalizationFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Globalization;

using FubarDev.FtpServer.Localization;

using JetBrains.Annotations;

using NGettext;

namespace FubarDev.FtpServer.Features
{
    public class LocalizationFeature : ILocalizationFeature
    {
        public LocalizationFeature([NotNull] IFtpCatalogLoader catalogLoader)
        {
            CatalogLoader = catalogLoader;
            Catalog = catalogLoader.DefaultCatalog;
            Language = catalogLoader.DefaultLanguage;
        }

        /// <inheritdoc />
        public IFtpCatalogLoader CatalogLoader { get; }

        /// <inheritdoc />
        public ICatalog Catalog { get; set; }

        /// <inheritdoc />
        public CultureInfo Language { get; set; }
    }
}
