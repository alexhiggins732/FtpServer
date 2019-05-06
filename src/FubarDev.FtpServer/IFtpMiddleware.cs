// <copyright file="IFtpMiddleware.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace FubarDev.FtpServer
{
    public interface IFtpMiddleware
    {
        Task InvokeAsync(FtpContext context, FtpRequestDelegate next);
    }
}
