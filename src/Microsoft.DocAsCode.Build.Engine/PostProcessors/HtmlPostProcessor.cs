﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class HtmlPostProcessor : IPostProcessor, ISupportIncrementalPostProcessor
    {
        public IPostProcessorHost PostProcessorHost { get; set; }

        public string GetIncrementalContextHash()
        {
            return null;
        }

        public List<HtmlDocumentHandler> Handlers { get; } = new List<HtmlDocumentHandler>();

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (outputFolder == null)
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }
            var context = HtmlPostProcessContext.Load(PostProcessorHost);
            foreach (var handler in Handlers)
            {
                handler.SetContext(context);
                manifest = handler.PreHandleWithScopeWrapper(manifest);
            }
            foreach (var tuple in from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                                  from output in item.OutputFiles
                                  where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                                  select new
                                  {
                                      Item = item,
                                      InputFile = item.SourceRelativePath,
                                      OutputFile = output.Value.RelativePath,
                                  })
            {
                if (!EnvironmentContext.FileAbstractLayer.Exists(tuple.OutputFile))
                {
                    continue;
                }
                var document = new HtmlDocument();
                try
                {
                    using (var stream = EnvironmentContext.FileAbstractLayer.OpenRead(tuple.OutputFile))
                    {
                        document.Load(stream, Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Warning: Can't load content from {tuple.OutputFile}: {ex.Message}");
                    continue;
                }
                foreach (var handler in Handlers)
                {
                    handler.HandleWithScopeWrapper(document, tuple.Item, tuple.InputFile, tuple.OutputFile);
                }
                using (var stream = EnvironmentContext.FileAbstractLayer.Create(tuple.OutputFile))
                {
                    document.Save(stream, Encoding.UTF8);
                }
            }
            foreach (var handler in Handlers)
            {
                manifest = handler.PostHandleWithScopeWrapper(manifest);
            }
            context.Save();
            return manifest;
        }
    }
}
