// <copyright file="FtpConnectionState.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Globalization;
using System.Text;

using FubarDev.FtpServer.Features;
using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.Localization;

using JetBrains.Annotations;

using Microsoft.AspNetCore.Http.Features;

using NGettext;

namespace FubarDev.FtpServer
{
    public class FtpConnectionState : IFtpConnectionState
    {
        public FtpConnectionState(
            [NotNull] IConnectionFeature connectionFeature,
            [NotNull] ILocalizationFeature localizationFeature)
        {
            Features.Set(connectionFeature);
            Features.Set(localizationFeature);
            Features.Set<IFtpFileSystemFeature>(new FtpFileSystemFeature());
        }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        /// <inheritdoc />
        public Address RemoteAddress => Features.Get<IConnectionFeature>().RemoteAddress;

        /// <inheritdoc />
        public Encoding Encoding
        {
            get => Features.Get<IConnectionFeature>().Encoding;
            set => Features.Get<IConnectionFeature>().Encoding = value;
        }

        /// <inheritdoc />
        public IFtpCatalogLoader CatalogLoader => Features.Get<ILocalizationFeature>().CatalogLoader;

        /// <inheritdoc />
        public ICatalog Catalog
        {
            get => Features.Get<ILocalizationFeature>().Catalog;
            set => Features.Get<ILocalizationFeature>().Catalog = value;
        }

        /// <inheritdoc />
        public CultureInfo Language
        {
            get => Features.Get<ILocalizationFeature>().Language;
            set => Features.Get<ILocalizationFeature>().Language = value;
        }

        /// <inheritdoc />
        public IUnixFileSystem FileSystem
        {
            get => Features.Get<IFtpFileSystemFeature>().FileSystem;
            set => Features.Get<IFtpFileSystemFeature>().FileSystem = value;
        }

        /// <inheritdoc />
        public Stack<IUnixDirectoryEntry> Path
        {
            get => Features.Get<IFtpFileSystemFeature>().Path;
            set => Features.Get<IFtpFileSystemFeature>().Path = value;
        }
    }
}
