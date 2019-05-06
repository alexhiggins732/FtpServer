// <copyright file="FtpClientException.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.FtpServer
{
    public class FtpClientException : FtpServerException
    {
        public FtpClientException()
        {
        }

        public FtpClientException(string message)
            : base(message)
        {
        }

        public FtpClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
