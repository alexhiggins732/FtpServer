// <copyright file="FtpServerException.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.FtpServer
{
    public class FtpServerException : Exception
    {
        public FtpServerException()
        {
        }

        public FtpServerException(string message)
            : base(message)
        {
        }

        public FtpServerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
