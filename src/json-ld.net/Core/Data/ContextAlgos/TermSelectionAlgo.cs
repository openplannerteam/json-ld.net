using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    /// <summary>
    /// Term Selection
    /// http://json-ld.org/spec/latest/json-ld-api/#term-selection
    /// This algorithm, invoked via the IRI Compaction algorithm, makes use of an
    /// active context's inverse context to find the term that is best used to
    /// compact an IRI.
    /// Other information about a value associated with the IRI
    /// is given, including which container mappings and which type mapping or
    /// language mapping would be best used to express the value.
    /// </summary>
    public static class TermSelectionAlgo
    {
        public static string SelectTerm(this Context activeContext, string iri, JArray containers, string typeLanguage,
            JArray preferredValues)
        {
            var inv = activeContext.GetInverse();
            var containerMap = (JObject) inv[iri];
            foreach (string container in containers)
            {
                if (!containerMap.ContainsKey(container))
                {
                    continue;
                }

                var typeLanguageMap = (JObject) containerMap[container];
                var valueMap = (JObject) typeLanguageMap[typeLanguage];
                foreach (string item in preferredValues)
                {
                    if (valueMap.ContainsKey(item))
                    {
                        return (string) valueMap[item];
                    }
                }
            }

            return null;
        }
    }
}