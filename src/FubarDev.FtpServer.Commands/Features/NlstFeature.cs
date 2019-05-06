// <copyright file="NlstFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Text;

namespace FubarDev.FtpServer.Features
{
    public class NlstFeature : INlstFeature
    {
        /// <inheritdoc />
        public Encoding Encoding { get; set; }
    }
}
