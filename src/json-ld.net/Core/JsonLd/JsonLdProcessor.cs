using System;
using JsonLD.Core.ProcessorAlgos;
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
        
        
        public JsonLdProcessor(IDocumentLoader loader) : this(loader, null)
        {
        }
        
        public JsonLdProcessor(IDocumentLoader loader, JsonLdOptions options)
        {
            this.loader = loader;

            this.options = options ?? new JsonLdOptions("");

            compactionExpansion = new CompactionExpansionAlgo(loader, this.options);
            flatten = new FlattenAlgo(options, compactionExpansion, loader);
        }

        /// <summary>
        /// Fetches the Json-LD found at the given uri and expand it fully for usage.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public JToken Load(Uri uri)
        {
            var raw = loader.LoadDocument(uri);
            return Expand(null, raw);
        }
        
        /// <summary>
        /// Compacts everything
        /// </summary>
        /// <param name="activeCtx"></param>
        /// <param name="activeProperty"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public JToken Compact(Context activeCtx, string activeProperty, JToken element)
        {
            return compactionExpansion.Compact(activeCtx, activeProperty, element, true);
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