// <copyright file="IServerStatusEventFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.FtpServer.Features
{
    public interface IServerStatusEventFeature
    {
        event EventHandler<FtpServerStatusEventArgs> Status;

        void OnStatus(FtpServerStatus status);
    }
}
