// <copyright file="IMlstFactsFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace FubarDev.FtpServer.Features
{
    public interface IMlstFactsFeature
    {
        ICollection<string> ActivaFacts { get; }
    }
}
