// <copyright file="IFtpFileSystemFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;

using FubarDev.FtpServer.FileSystem;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.Features
{
    public interface IFtpFileSystemFeature
    {
        [NotNull]
        IUnixFileSystem FileSystem { get; set; }

        [NotNull]
        Stack<IUnixDirectoryEntry> Path { get; set; }
    }
}
