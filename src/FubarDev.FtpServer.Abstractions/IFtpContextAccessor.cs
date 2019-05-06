// <copyright file="IFtpContextAccessor.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.FtpServer
{
    public interface IFtpContextAccessor
    {
        FtpContext FtpContext { get; set; }
    }
}
