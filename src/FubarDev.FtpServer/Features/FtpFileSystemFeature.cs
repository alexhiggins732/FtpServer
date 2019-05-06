// <copyright file="FtpFileSystemFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

using FubarDev.FtpServer.FileSystem;

namespace FubarDev.FtpServer.Features
{
    public class FtpFileSystemFeature : IFtpFileSystemFeature
    {
        private IUnixFileSystem _fileSystem;

        /// <inheritdoc />
        public IUnixFileSystem FileSystem
        {
            get => _fileSystem ?? throw new InvalidOperationException("No file system provisioned.");
            set => _fileSystem = value;
        }

        /// <inheritdoc />
        public Stack<IUnixDirectoryEntry> Path { get; set; } = new Stack<IUnixDirectoryEntry>();
    }
}
