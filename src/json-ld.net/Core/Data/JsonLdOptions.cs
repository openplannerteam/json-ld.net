using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    /// <summary>
    /// An object which keeps track of a few variables. The variables state what options are used.
    /// Absolutely nothing interesting is happening here.
    /// Also see http://json-ld.org/spec/latest/json-ld-api/#the-jsonldoptions-type
    /// </summary>
    public class JsonLdOptions
    {
        public IDocumentLoader documentLoader = null;
        // TODO: THE FOLLOWING ONLY EXIST SO I DON'T HAVE TO DELETE A LOT OF CODE,
        // REMOVE IT WHEN DONE


        public string Format = null;

        public const bool UseNamespaces = false;

        public readonly string OutputForm = null;

        public string Base = null;


        public JObject ExpandContext = null;

        public string ProcessingMode = "json-ld-1.0";

        public bool? Embed, Explicit, OmitDefault = null;

        public bool CompactArrays = true;
        public bool UseRdfType, UseNativeTypes, ProduceGeneralizedRdf = false;

        public JsonLdOptions() : this(string.Empty)
        {
        }

        public JsonLdOptions(string @base)
        {
            Base = @base;
        }

        public JsonLdOptions Clone()
        {
            return new JsonLdOptions(Base);
        }
    }
}