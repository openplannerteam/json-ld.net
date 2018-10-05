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
    public class JsonLdProcessorClean
    {
        private readonly IDocumentLoader loader;
        private readonly CompactionExpansionAlgo compactionExpansion;
        private readonly JsonLdOptions options;

        
        
        public JsonLdProcessorClean(IDocumentLoader loader) : this(loader, null)
        {
        }

        public JsonLdProcessorClean(IDocumentLoader loader, JsonLdOptions options)
        {
            this.loader = loader;
            if (options == null)
            {
                options = new JsonLdOptions("");
            }

            this.options = options;

            compactionExpansion = new CompactionExpansionAlgo(loader, this.options);
        }


        public JObject Compact(JToken input, JToken context)
        {
           return compactionExpansion.Compact(input, context);
        }

        public JArray Expand(JToken input)
        {
            return compactionExpansion.Expand(input, options);
        }

       
    }
}