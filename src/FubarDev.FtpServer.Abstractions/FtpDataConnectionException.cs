// <copyright file="FtpDataConnectionException.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.FtpServer
{
    public class FtpDataConnectionException : FtpClientException
    {
        public FtpDataConnectionException()
        {
        }

        public FtpDataConnectionException(string message)
            : base(message)
        {
        }

        public FtpDataConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
