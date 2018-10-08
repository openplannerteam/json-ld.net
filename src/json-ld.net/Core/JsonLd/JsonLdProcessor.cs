using System;
using JsonLD.Core.ContextAlgos;
using JsonLD.Core.ProcessorAlgos;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    /// <summary>
    /// The JSON-LD Processor is the main entry point for a user of the library.
    /// The user creates a JSonLdProcessor-object which does all the modifications
    ///
    /// The interface is described here:
    /// http://json-ld.org/spec/latest/json-ld-api/#the-jsonldprocessor-interface
    /// 
    /// // TODO Move all functionality from the old JsonLdProcessor here, remove the old one and rename this one
    /// </summary>
    public class JsonLdProcessor
    {
        private readonly IDocumentLoader loader;
        private readonly JsonLdOptions options;

        private readonly CompactionExpansionAlgo compactionExpansion;
        private readonly FlattenAlgo flatten;

        public JsonLdProcessor(IDocumentLoader loader, Uri basePath) :
            this(loader, new JsonLdOptions(basePath.Scheme + "://" + basePath.Host))
        {
        }


        public JsonLdProcessor(IDocumentLoader loader, JsonLdOptions options)
        {
            this.loader = loader;

            this.options = options;

            compactionExpansion = new CompactionExpansionAlgo(loader, this.options);
            flatten = new FlattenAlgo(options, compactionExpansion, loader);
        }

        /// <summary>
        /// Fetches the Json-LD found at the given uri and expand it fully for usage.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public JToken LoadExpanded(Uri uri)
        {
            var raw = loader.LoadDocument(uri);
            var ctx = new Context(options);
            return Expand(ctx, raw);
        }

        public JToken Load(Uri uri)
        {
            return loader.LoadDocument(uri);
        }

        public Context ExtractContext(JToken json)
        {
            var parser = new ParsingAlgorithm(new Context(options), loader);
            return parser.ParseContext(json);
        }


        public JToken Compact(JToken element)
        {
            return Compact(new Context(options), element);
        }

        public JToken Compact(Context activeCtx, JToken element)
        {
            return compactionExpansion.Compact(activeCtx, null, element, true);
        }


        public JObject CompactContext(JToken input, JToken context)
        {
            return compactionExpansion.CompactContext(input, context);
        }

        public JArray ExpandContext(JToken input)
        {
            return compactionExpansion.ExpandContext(input, options);
        }

        public JToken Expand(Context activeCtx, JToken token)
        {
            return compactionExpansion.Expand(activeCtx, null, token);
        }

        public JToken Flatten(JToken input, JToken context)
        {
            return flatten.Flatten(input, context);
        }
    }
}