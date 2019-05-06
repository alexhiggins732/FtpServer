// <copyright file="TransparentDataConnectionFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.FtpServer.Features
{
    public class TransparentDataConnectionFeature : ITransparentDataConnectionFeature
    {
        /// <inheritdoc />
        public string TransferTypeCommandUsed { get; set; }

        /// <inheritdoc />
        public Address PortCommandAddress { get; set; }
    }
}
