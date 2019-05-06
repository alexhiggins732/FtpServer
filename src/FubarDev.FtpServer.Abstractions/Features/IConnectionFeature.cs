// <copyright file="IConnectionFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Text;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace FubarDev.FtpServer.Features
{
    public interface IConnectionFeature
    {
        [NotNull]
        Address LocalAddress { get; }

        [NotNull]
        Address RemoteAddress { get; }

        [NotNull]
        Encoding Encoding { get; set; }

        [CanBeNull]
        ILogger Logger { get; }
    }
}
