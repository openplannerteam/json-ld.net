using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    /// <summary>
    /// Implements the 'inverse context creation'
    ///
    /// When there is more than one term that could be chosen to compact an IRI, it has to be ensured that the term selection is both deterministic and represents the most context-appropriate choice whilst taking into consideration algorithmic complexity.
    ///
    /// In order to make term selections, the concept of an inverse context is introduced. An inverse context is essentially a reverse lookup table that maps container mapping, type mappings, and language mappings to a simple term for a given active context. A inverse context only needs to be generated for an active context if it is being used for compaction.
    ///
    /// To make use of an inverse context, a list of preferred container mapping and the type mapping or language mapping are gathered for a particular value associated with an IRI. These parameters are then fed to the Term Selection algorithm, which will find the term that most appropriately matches the value's mappings.
    ///
    /// See https://json-ld.org/spec/latest/json-ld-api/#inverse-context-creation 
    /// </summary>
    public static class CreateInverseAlgo
    {
        private class Comparer794 : IComparer<JToken>
        {
            public int Compare(JToken a, JToken b)
            {
                return JsonLdUtils.CompareShortestLeast((string) a, (string) b);
            }
        }

        internal static JObject CreateInverse(this Context activeContext)
        {
            // 1)
            var inverse = new JObject();
            // 2)

            // TODO This is unused. Why?
            var defaultLanguage = (string) activeContext["@language"]
                                  ?? "@none";

            // create term selections for each mapping in the context, ordered by
            // shortest and then lexicographically least
            var terms = new JArray(activeContext.TermDefinitions.GetKeys());
            terms.SortInPlace(new Comparer794());

            foreach (string term in terms)
            {
                var definitionToken = activeContext.TermDefinitions[term];
                // 3.1)
                if (definitionToken.IsNull())
                {
                    continue;
                }

                var definition = (JObject) activeContext.TermDefinitions[term];
                // 3.2)
                var container = (string) definition["@container"]
                                ?? "@none";


                // 3.3)
                var iri = (string) definition["@id"];
                // 3.4 + 3.5)
                var containerMap = (JObject) inverse[iri] ?? new JObject();
                inverse[iri] = containerMap;

                // 3.6 + 3.7)
                var typeLanguageMap = (JObject) containerMap[container];
                if (typeLanguageMap == null)
                {
                    typeLanguageMap = new JObject
                    {
                        ["@language"] = new JObject(),
                        ["@type"] = new JObject()
                    };
                    containerMap[container] = typeLanguageMap;
                }


                // 3.8)
                if (definition["@reverse"].SafeCompare(true))
                {
                    var typeMap = (JObject) typeLanguageMap["@type"];
                    if (!typeMap.ContainsKey("@reverse"))
                    {
                        typeMap["@reverse"] = term;
                    }
                }
                else if (definition.ContainsKey("@type"))
                {
                    // 3.9)
                    var typeMap = (JObject) typeLanguageMap["@type"];
                    if (!typeMap.ContainsKey((string) definition["@type"]))
                    {
                        typeMap[(string) definition["@type"]] = term;
                    }
                }
                else if (definition.ContainsKey("@language"))
                {
                    var languageMap = (JObject) typeLanguageMap["@language"];
                    var language = (string) definition["@language"]
                                   ?? "@null";

                    if (!languageMap.ContainsKey(language))
                    {
                        languageMap[language] = term;
                    }
                }
                else
                {
                    // 3.11)
                    // 3.11.1)
                    var languageMap = (JObject) typeLanguageMap["@language"];
                    // 3.11.2)
                    if (!languageMap.ContainsKey("@language"))
                    {
                        languageMap["@language"] = term;
                    }

                    // 3.11.3)
                    if (!languageMap.ContainsKey("@none"))
                    {
                        languageMap["@none"] = term;
                    }

                    // 3.11.4)
                    var typeMap = (JObject) typeLanguageMap["@type"];
                    // 3.11.5)
                    if (!typeMap.ContainsKey("@none"))
                    {
                        typeMap["@none"] = term;
                    }
                }
            }

            // 4)
            return inverse;
        }
    }
}