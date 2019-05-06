// <copyright file="IFtpConnectionState.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using FubarDev.FtpServer.Features;

using Microsoft.AspNetCore.Http.Features;

namespace FubarDev.FtpServer
{
    public interface IFtpConnectionState : IConnectionFeature, ILocalizationFeature, IFtpFileSystemFeature
    {
        IFeatureCollection Features { get; }
    }
}
