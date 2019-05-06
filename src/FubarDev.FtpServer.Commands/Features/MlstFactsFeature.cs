// <copyright file="MlstFactsFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace FubarDev.FtpServer.Features
{
    public class MlstFactsFeature : IMlstFactsFeature
    {
        /// <inheritdoc />
        public ICollection<string> ActivaFacts { get; } = new List<string>();
    }
}
