using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    /// <summary>
    /// This algorithm is called from the Context Processing algorithm to create a term definition
    /// in the active context for a term being processed in a local context.
    /// http://json-ld.org/spec/latest/json-ld-api/#create-term-definition
    /// </summary>
    public static class CreateTermDefinitionAlgorithm
    {
        public static void CreateTermDefinition(this Context activeContext, JObject localContext, string term
            , IDictionary<string, bool> defined)
        {
            if (defined.ContainsKey(term))
            {
                if (defined[term])
                {
                    return;
                }

                throw new JsonLdError(JsonLdError.Error.CyclicIriMapping, term);
            }

            defined[term] = false;
            if (JsonLd.IsKeyword(term))
            {
                throw new JsonLdError(JsonLdError.Error.KeywordRedefinition, term);
            }

            Collections.Remove(activeContext.TermDefinitions, term);
            var value = localContext[term];
            if (value.IsNull() || 
                (value is JObject o 
                 && o.ContainsKey("@id")
                 && o["@id"].IsNull()))
            {
                activeContext.TermDefinitions[term] = null;
                defined[term] = true;
                return;
            }

            if (value.IsString())
            {
               value = new JObject {["@id"] = value};
            }

            if (!(value is JObject))
            {
                throw new JsonLdError(JsonLdError.Error.InvalidTermDefinition, value);
            }

            // casting the value so it doesn't have to be done below every time
            var val = (JObject) value;
            // 9) create a new term definition
            var definition = new JObject();
            // 10)
            if (val.ContainsKey("@type"))
            {
                if (!val["@type"].IsString())
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidTypeMapping, val["@type"]);
                }

                var type = (string) val["@type"];
                try
                {
                    type = activeContext.ExpandIri((string) val["@type"], false, true, localContext, defined);
                }
                catch (JsonLdError error)
                {
                    if (error.GetType() != JsonLdError.Error.InvalidIriMapping)
                    {
                        throw;
                    }

                    throw new JsonLdError(JsonLdError.Error.InvalidTypeMapping, type);
                }

                // TODO: fix check for absoluteIri (blank nodes shouldn't count, at least not here!)
                if ("@id".Equals(type) 
                    || "@vocab".Equals(type)
                    || (!type.StartsWith("_:")
                        && type.IsAbsoluteIri()))
                {
                    definition["@type"] = type;
                }
                else
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidTypeMapping, type);
                }
            }

            // 11)
            if (val.ContainsKey("@reverse"))
            {
                if (val.ContainsKey("@id"))
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidReverseProperty, val);
                }

                if (!val["@reverse"].IsString())
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidIriMapping,
                        "Expected String for @reverse value. got "
                        + (val["@reverse"].IsNull() ? "null" : val["@reverse"].GetType().ToString()));
                }

                var reverse = activeContext.ExpandIri((string) val["@reverse"], false, true, localContext, defined
                );
                if (!reverse.IsAbsoluteIri())
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "Non-absolute @reverse IRI: "
                                                                               + reverse);
                }

                definition["@id"] = reverse;
                if (val.ContainsKey("@container"))
                {
                    var container = (string) val["@container"];
                    if (container == null 
                        || "@set".Equals(container)
                        || "@index".Equals(container))
                    {
                        definition["@container"] = container;
                    }
                    else
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidReverseProperty,
                            "reverse properties only support set- and index-containers"
                        );
                    }
                }

                definition["@reverse"] = true;
                activeContext.TermDefinitions[term] = definition;
                defined[term] = true;
                return;
            }

            // 12)
            definition["@reverse"] = false;
            // 13)
            if (!val["@id"].IsNull() 
                && !val["@id"].SafeCompare(term))
            {
                if (!val["@id"].IsString())
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "expected value of @id to be a string");
                }

                var res = activeContext.ExpandIri((string) val["@id"], false, true, localContext, defined);
                if (JsonLd.IsKeyword(res) || res.IsAbsoluteIri())
                {
                    if ("@context".Equals(res))
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidKeywordAlias, "cannot alias @context"
                        );
                    }

                    definition["@id"] = res;
                }
                else
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidIriMapping,
                        "resulting IRI mapping should be a keyword, absolute IRI or blank node"
                    );
                }
            }
            else
            {
                // 14)
                if (term.IndexOf(":", StringComparison.Ordinal) >= 0)
                {
                    var colIndex = term.IndexOf(":", StringComparison.Ordinal);
                    var prefix = term.Substring(0, colIndex);
                    var suffix = term.Substring(colIndex + 1);
                    if (localContext.ContainsKey(prefix))
                    {
                        activeContext.CreateTermDefinition(localContext, prefix, defined);
                    }

                    if (activeContext.TermDefinitions.ContainsKey(prefix))
                    {
                        definition["@id"] = (string) (((IDictionary<string, JToken>) activeContext. TermDefinitions[prefix])["@id"]) +
                                            suffix;
                    }
                    else
                    {
                        definition["@id"] = term;
                    }
                }
                else
                {
                    // 15)
                    if (activeContext.ContainsKey("@vocab"))
                    {
                        definition["@id"] = (string) activeContext["@vocab"] + term;
                    }
                    else
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidIriMapping,
                            "relative term definition without vocab mapping"
                        );
                    }
                }
            }

            // 16)
            if (val.ContainsKey("@container"))
            {
                var container = (string) val["@container"];
                if (!"@list".Equals(container) && !"@set".Equals(container) && !"@index".Equals(container
                    ) && !"@language".Equals(container))
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidContainerMapping,
                        "@container must be either @list, @set, @index, or @language"
                    );
                }

                definition["@container"] = container;
            }

            // 17)
            if (val.ContainsKey("@language") && !val.ContainsKey("@type"))
            {
                if (val["@language"].IsNull())
                {
                    definition["@language"] = null;
                }else
                if (val["@language"].IsString())
                {
                    definition["@language"] =  val["@language"].ToString().ToLower();
                }
                else
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidLanguageMapping, "@language must be a string or null");
                }
            }

            // 18)
            activeContext.TermDefinitions[term] = definition;
            defined[term] = true;
        }
    }
}