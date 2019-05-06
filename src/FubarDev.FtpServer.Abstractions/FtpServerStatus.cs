// <copyright file="FtpServerStatus.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.FtpServer
{
    public enum FtpServerStatus
    {
        TlsDisabled,
        TlsWasDisabled,
        TlsEnabled,
        TlsEnableErrorNotConfigured,
        TlsEnableError,
    }
}
