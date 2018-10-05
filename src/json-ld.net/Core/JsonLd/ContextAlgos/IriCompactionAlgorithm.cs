using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    public static class IriCompactionAlgorithm
    {
        public static string CompactIri(this Context activeContext, string iri, bool relativeToVocab)
        {
            return activeContext.CompactIri(iri, null, relativeToVocab, false);
        }

        public static string CompactIri(this Context activeContext, string iri)
        {
            return activeContext.CompactIri(iri, false);
        }

        /// <summary>
        /// IRI Compaction Algorithm
        /// http://json-ld.org/spec/latest/json-ld-api/#iri-compaction
        /// Compacts an IRI or keyword into a term or prefix if it can be.
        /// </summary>
        /// <param name="activeContext">The context in which this iri is compacted</param>
        /// <param name="iri">the IRI to compact. Known as 'var' in the spec</param>
        /// <param name="value">The value giving more information about the IRI. Can be null.</param>
        /// <param name="relativeToVocab"> Option on how to compact the IRI: vocab: true to split after
        /// false not to.
        /// </param>
        /// <param name="reverse">true if a reverse property is being compacted, false if not.</param>
        /// <returns>the compacted term, prefix, keyword alias, or the original IRI.</returns>
        public static string CompactIri(this Context activeContext,
            string iri, JToken value, bool relativeToVocab, bool reverse)
        {
            // 1)
            if (iri == null)
            {
                return null;
            }

            // 2)
            if (relativeToVocab && activeContext.GetInverse().ContainsKey(iri))
            {
                var term = activeContext.HandleVocabCase(iri, value, reverse);
                // 2.19
                if (term != null)
                {
                    return term;
                }
            }

            // 3)
            if (relativeToVocab && activeContext.ContainsKey("@vocab"))
            {
                var vocab = activeContext["@vocab"].ToString();
                // determine if vocab is a prefix of the iri
                // 3.1)
                if (iri.StartsWith(vocab) && vocab.Length < iri.Length)
                {
                    // use suffix as relative iri if it is not a term in the
                    // active context
                    var suffix = iri.Substring(vocab.Length);
                    if (!activeContext.TermDefinitions.ContainsKey(suffix))
                    {
                        return suffix;
                    }
                }
            }

            // 4)
            string compactIri = null;
            // 5)
            foreach (var term1 in activeContext.TermDefinitions.GetKeys())
            {
                var termDefinitionToken = activeContext.TermDefinitions[term1];
                // 5.1)
                if (termDefinitionToken.IsNull()
                    || termDefinitionToken.Equals(value)
                    || !iri.StartsWith(termDefinitionToken.ToString())
                    // TODO JSON-LD 1.1:  or the term definition does not contain the prefix flag having a value of true,
                    )
                {
                    // This term1 can not be used to compact the IRI
                    // We continue to the next term
                    continue;
                }
                
                if (term1.Contains(":"))
                {
                    // TODO This piece was already here, but not referenced in the spec
                    continue;
                }

                
                
                var termDefinition = (JObject) termDefinitionToken;
                var iriMapping = termDefinition["@id"];
                
                
                if (termDefinition["@id"].SafeCompare(iri)
                    || !iri.StartsWith(iriMapping.ToString()))
                    
                {
                    continue;
                }
                

                // 5.2)
                var candidate = term1 + ":" + iri.Substring(iriMapping.Count());
                
                // 5.3)
                if ((compactIri == null
                       || JsonLdUtils.CompareShortestLeast(candidate, compactIri) < 0)
                    && (!activeContext.TermDefinitions.ContainsKey(candidate) 
                        || ((IDictionary<string, JToken>) activeContext.TermDefinitions[candidate])["@id"].SafeCompare(iri) 
                        && value.IsNull())
                    )
                {
                    compactIri = candidate;
                }
            }

            // 6)
            if (compactIri != null)
            {
                return compactIri;
            }

            // 7)
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!relativeToVocab)
            {
                return URL.RemoveBase(activeContext["@base"], iri);
            }

            // 8)
            return iri;
        }


        // Handle case 2
        private static string HandleVocabCase(this Context activeContext, string iri, JToken value, bool reverse)
        {
            // 2.1)
            var defaultLanguage = activeContext["@language"] ?? "@none";

            // 2.2
            if (value.IsDictContaining("@preserve"))
            {
                value = value["@preserve"][0];
            }

            // 2.3)
            var containers = new JArray();
            // 2.4)
            var typeLanguage = "@language";
            var typeLanguageValue = "@null";
            // 2.5)
            if (value.IsDictContaining("@index"))
            {
                containers.Add("@index");
                containers.Add("@index@set");
            }

            // 2.6)
            if (reverse)
            {
                typeLanguage = "@type";
                typeLanguageValue = "@reverse";
                containers.Add("@set");
            }
            else
            {
                // 2.7)
                if (value.IsDictContaining("@list", out var dict))
                {
                    // 2.7.1)
                    if (!dict.ContainsKey("@index"))
                    {
                        containers.Add("@list");
                    }

                    // 2.7.2)
                    var list = (JArray) dict["@list"];
                    // 2.7.3)
                    string commonType = null;
                    var commonLanguage = list.IsEmpty() ? defaultLanguage.ToString() : null;

                    // 2.7.4)
                    foreach (var item in list)
                    {
                        // 2.7.4.1)
                        var itemLanguage = "@none";
                        var itemType = "@none";
                        // 2.7.4.2)
                        if (item.IsDictContaining("@value", out var itemDict))
                        {
                            // 2.7.4.2.1)
                            if (itemDict.ContainsKey("@language"))
                            {
                                itemLanguage = (string) dict["@language"];
                            }
                            else if (itemDict.ContainsKey("@type"))
                            {
                                // 2.7.4.2.2)
                                itemType = itemDict["@type"].ToString();
                            }
                            else
                            {
                                // 2.7.4.2.3)
                                itemLanguage = "@null";
                            }
                        }
                        else
                        {
                            // 2.7.4.3)
                            itemType = "@id";
                        }

                        // 2.7.4.4)
                        if (commonLanguage == null)
                        {
                            commonLanguage = itemLanguage;
                        }
                        else if (!commonLanguage.Equals(itemLanguage) && item.IsDictContaining("@value"))
                        {
                            // 2.7.4.5)
                            commonLanguage = "@none";
                        }

                        // 2.7.4.6)
                        if (commonType == null)
                        {
                            commonType = itemType;
                        }
                        else if (!commonType.Equals(itemType))
                        {
                            // 2.7.4.7)
                            commonType = "@none";
                        }

                        // 2.7.4.8)
                        if ("@none".Equals(commonLanguage) && "@none".Equals(commonType))
                        {
                            break;
                        }
                    } // End of the for-each element loop

                    // 2.7.5)
                    commonLanguage = commonLanguage ?? "@none";
                    // 2.7.6)
                    commonType = commonType ?? "@none";


                    // 2.7.7)
                    if (!"@none".Equals(commonType))
                    {
                        typeLanguage = "@type";
                        typeLanguageValue = commonType;
                    }
                    else
                    {
                        // 2.7.8)
                        typeLanguageValue = commonLanguage;
                    }
                }
                else if (value is JObject graph && graph.ContainsKey("@graph"))
                {
                    // 2.8: Graph object handling
                    // TODO JSON-LD 1.1

                    /*
                    containers.Add("@graph@index");
                    containers.Add("@graph@index@set");

                    containers.Add("@graph@id");
                    containers.Add("@graph@id@set");

                    containers.Add("@graph");
                    containers.Add("@graph@set");
                    containers.Add("@set");
                    containers.Add("@index");
                    containers.Add("@index@set");*/
                }

                else if (value.IsDictContaining("@value", out var valueDict))
                {
                    // 2.9)
                    if (valueDict.ContainsKey("@language")
                        && !valueDict.ContainsKey("@index"))
                    {
                        // 2.9.1.1)
                        typeLanguageValue = valueDict["@language"].ToString();
                        containers.Add("@language");
                    }
                    else if (valueDict.ContainsKey("@type"))
                    {
                        // 2.9.1.2)
                        typeLanguage = "@type";
                        typeLanguageValue = valueDict["@type"].ToString();
                    }
                }
                else
                {
                    // 2.9.2)
                    typeLanguage = "@type";
                    typeLanguageValue = "@id";

                    // JSON-LD 1.1
                    containers.Add("@id");
                    containers.Add("@id@set");
                    containers.Add("@type");
                    containers.Add("@set@type");
                }

                // 2.9.3)


                containers.Add("@set");
            }

            // 2.10)
            containers.Add("@none");


            // TODO Implement 2.11 and 2.12 for JSON-LD 1.1

            // 2.13)
            typeLanguageValue = typeLanguageValue ?? "@null";

            // 2.14)
            var preferredValues = new JArray();

            // 2.15)
            if ("@reverse".Equals(typeLanguageValue))
            {
                preferredValues.Add("@reverse");
            }

            // 2.16)
            if (("@reverse".Equals(typeLanguageValue) || "@id".Equals(typeLanguageValue))
                && value is JObject o
                && o.ContainsKey("@id"))
            {
                // 2.16.1)
                var result = activeContext.CompactIri(o["@id"].ToString(), null, true, true);

                var termDefs = activeContext.TermDefinitions.ContainsKey(result)
                    ? (IDictionary<string, JToken>) activeContext.TermDefinitions[result]
                    : null;
                if (termDefs != null
                    && termDefs.ContainsKey("@id")
                    && o["@id"].SafeCompare(termDefs["@id"]))
                {
                    preferredValues.Add("@vocab");
                    preferredValues.Add("@id");
                }
                else
                {
                    // 2.16.2)
                    preferredValues.Add("@id");
                    preferredValues.Add("@vocab");
                }
            }
            else
            {
                // 2.17)
                preferredValues.Add(typeLanguageValue);
                preferredValues.Add("@none");
                if (value.IsDictContaining("@list") && !value.GetContents("@list").Any())
                {
                    typeLanguage = "@any"; // JSON-LD 1.1
                }
            }


            // 2.18)
            // TODO according to spec, "inverse context" should be passed as well
            var term = activeContext.SelectTerm(iri, containers, typeLanguage, preferredValues);

            // 2.19
            return term;
        }
    }
}