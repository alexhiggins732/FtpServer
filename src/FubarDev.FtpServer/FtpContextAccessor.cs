// <copyright file="FtpContextAccessor.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.FtpServer
{
    public class FtpContextAccessor : IFtpContextAccessor
    {
        /// <inheritdoc />
        public FtpContext FtpContext { get; set; }
    }
}
