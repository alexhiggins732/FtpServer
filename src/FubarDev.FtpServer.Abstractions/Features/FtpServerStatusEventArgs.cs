// <copyright file="FtpServerStatusEventArgs.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.FtpServer.Features
{
    public class FtpServerStatusEventArgs : EventArgs
    {
        public FtpServerStatusEventArgs(FtpServerStatus status)
        {
            Status = status;
        }

        public FtpServerStatus Status { get; }
    }
}
