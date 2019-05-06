// <copyright file="INlstFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Text;

namespace FubarDev.FtpServer.Features
{
    public interface INlstFeature
    {
        Encoding Encoding { get; set; }
    }
}
