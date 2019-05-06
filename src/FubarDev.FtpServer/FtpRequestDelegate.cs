// <copyright file="FtpRequestDelegate.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace FubarDev.FtpServer
{
    public delegate Task FtpRequestDelegate(FtpContext context);
}
