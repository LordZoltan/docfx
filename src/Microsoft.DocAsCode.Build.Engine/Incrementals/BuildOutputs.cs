﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;

    public class BuildOutputs
    {
        /// <summary>
        /// The items of output.
        /// </summary>
        public List<BuildOutputItem> Items { get; set; }
    }
}
