using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using JsonLD.Core.ContextAlgos;

namespace JsonLD.Core
{
    /// <summary>
    ///
    /// The 'Context'-part of a JSON-LD document gives some common terms and definitions which are used in the
    /// latter of the document to refer efficiently to terms.
    ///
    /// In other words, the context enables some compression by giving a common dictionary.
    ///
    /// For example, the JSON may be
    ///
    /// {
    ///     "@context": {
    ///         "name": "http://schema.org/name",
    ///         "image": {
    ///             "@id": "http://schema.org/image",
    ///             "@type": "@id"
    ///         },
    ///         "homepage": {
    ///             "@id": "http://schema.org/url",
    ///             "@type": "@id"
    ///         }
    ///     },
    ///     "name": "Mano Sporty",
    ///     "homepage": "http://manu.sporny.org/",
    ///     "image": "http://manu.sporny.org/images/manu.png"
    /// }
    ///
    /// This would be equivalent to writing:
    ///
    ///     /// {
    ///     "@context": {
    ///         "name": "http://schema.org/name",
    ///         "image": {
    ///             "@id": "http://schema.org/image",
    ///             "@type": "@id"
    ///         },
    ///         "homepage": {
    ///             "@id": "http://schema.org/url",
    ///             "@type": "@id"
    ///         }
    ///     },
    ///     "http://schema.org/name": "Mano Sporty",
    ///     "http://schema.org/image": {"@id": "http://manu.sporny.org/"},
    ///     "http://schema.org/url": {@id: "http://manu.sporny.org/images/manu.png"}
    /// }
    ///
    ///
    /// 
    /// Also see: https://json-ld.org/spec/FCGS/json-ld/20180607/#the-context
    /// The above examples come from the latter website as well.
    ///
    /// This class contains the data structures and minimal support functions for the context.
    /// More advanced algorithms (expansion, compaction, ...) can be found in ContextAlgos
    ///
    /// 
    /// </summary>
    public class Context : JObject
    {
        public JsonLdOptions _options;

        public JObject _termDefinitions;

        public JObject Inverse;

        public Context() : this(new JsonLdOptions())
        {
        }

        public Context(JsonLdOptions options)
        {
            Init(options);
        }

        public Context(JObject map, JsonLdOptions options) : base(map)
        {
            Init(options);
        }

        public Context(JObject map) : base(map)
        {
            Init(new JsonLdOptions());
        }

        public Context(JToken context, JsonLdOptions opts) : base(context is JObject ? (JObject) context : null)
        {
            Init(opts);
        }


        // TODO: load remote context
        private void Init(JsonLdOptions options)
        {
            _options = options;
            if (options.GetBase() != null)
            {
                this["@base"] = options.GetBase();
            }

            _termDefinitions = new JObject();
        }

        
     

        public Context Clone()
        {
            return new Context(DeepClone(), _options)
                {_termDefinitions = (JObject) _termDefinitions.DeepClone()};
        }

        /// <summary>
        /// Inverse Context Creation
        /// http://json-ld.org/spec/latest/json-ld-api/#inverse-context-creation
        /// Generates an inverse context for use in the compaction algorithm, if not
        /// already generated for the given active context.
        /// </summary>
        /// <remarks>
        /// Inverse Context Creation
        /// http://json-ld.org/spec/latest/json-ld-api/#inverse-context-creation
        /// Generates an inverse context for use in the compaction algorithm, if not
        /// already generated for the given active context.
        /// </remarks>
        /// <returns>the inverse context.</returns>
        public virtual JObject GetInverse()
        {
            // lazily create inverse
            if (Inverse != null)
            {
                return Inverse;
            }

            // 1)
            Inverse = new JObject();
            // 2)
            string defaultLanguage = (string) this["@language"];
            if (defaultLanguage == null)
            {
                defaultLanguage = "@none";
            }

            // create term selections for each mapping in the context, ordererd by
            // shortest and then lexicographically least
            JArray terms = new JArray(_termDefinitions.GetKeys());
            ((JArray) terms).SortInPlace(new _IComparer_794());
            foreach (string term in terms)
            {
                JToken definitionToken = _termDefinitions[term];
                // 3.1)
                if (definitionToken.Type == JTokenType.Null)
                {
                    continue;
                }

                JObject definition = (JObject) _termDefinitions[term];
                // 3.2)
                string container = (string) definition["@container"];
                if (container == null)
                {
                    container = "@none";
                }

                // 3.3)
                string iri = (string) definition["@id"];
                // 3.4 + 3.5)
                JObject containerMap = (JObject) Inverse[iri];
                if (containerMap == null)
                {
                    containerMap = new JObject();
                    Inverse[iri] = containerMap;
                }

                // 3.6 + 3.7)
                JObject typeLanguageMap = (JObject) containerMap[container];
                if (typeLanguageMap == null)
                {
                    typeLanguageMap = new JObject();
                    typeLanguageMap["@language"] = new JObject();
                    typeLanguageMap["@type"] = new JObject();
                    containerMap[container] = typeLanguageMap;
                }

                // 3.8)
                if (definition["@reverse"].SafeCompare(true))
                {
                    JObject typeMap = (JObject) typeLanguageMap
                        ["@type"];
                    if (!typeMap.ContainsKey("@reverse"))
                    {
                        typeMap["@reverse"] = term;
                    }
                }
                else
                {
                    // 3.9)
                    if (definition.ContainsKey("@type"))
                    {
                        JObject typeMap = (JObject) typeLanguageMap["@type"];
                        if (!typeMap.ContainsKey((string) definition["@type"]))
                        {
                            typeMap[(string) definition["@type"]] = term;
                        }
                    }
                    else
                    {
                        // 3.10)
                        if (definition.ContainsKey("@language"))
                        {
                            JObject languageMap = (JObject) typeLanguageMap
                                ["@language"];
                            string language = (string) definition["@language"];
                            if (language == null)
                            {
                                language = "@null";
                            }

                            if (!languageMap.ContainsKey(language))
                            {
                                languageMap[language] = term;
                            }
                        }
                        else
                        {
                            // 3.11)
                            // 3.11.1)
                            JObject languageMap = (JObject) typeLanguageMap
                                ["@language"];
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
                            JObject typeMap = (JObject) typeLanguageMap
                                ["@type"];
                            // 3.11.5)
                            if (!typeMap.ContainsKey("@none"))
                            {
                                typeMap["@none"] = term;
                            }
                        }
                    }
                }
            }

            // 4)
            return Inverse;
        }

        private sealed class _IComparer_794 : IComparer<JToken>
        {
            public _IComparer_794()
            {
            }

            public int Compare(JToken a, JToken b)
            {
                return JsonLdUtils.CompareShortestLeast((string) a, (string) b);
            }
        }

        /// <summary>
        /// Term Selection
        /// http://json-ld.org/spec/latest/json-ld-api/#term-selection
        /// This algorithm, invoked via the IRI Compaction algorithm, makes use of an
        /// active context's inverse context to find the term that is best used to
        /// compact an IRI.
        /// </summary>
        /// <remarks>
        /// Term Selection
        /// http://json-ld.org/spec/latest/json-ld-api/#term-selection
        /// This algorithm, invoked via the IRI Compaction algorithm, makes use of an
        /// active context's inverse context to find the term that is best used to
        /// compact an IRI. Other information about a value associated with the IRI
        /// is given, including which container mappings and which type mapping or
        /// language mapping would be best used to express the value.
        /// </remarks>
        /// <returns>the selected term.</returns>
        public string SelectTerm(string iri, JArray containers, string typeLanguage
            , JArray preferredValues)
        {
            JObject inv = GetInverse();
            // 1)
            JObject containerMap = (JObject) inv[iri];
            // 2)
            foreach (string container in containers)
            {
                // 2.1)
                if (!containerMap.ContainsKey(container))
                {
                    continue;
                }

                // 2.2)
                JObject typeLanguageMap = (JObject) containerMap
                    [container];
                // 2.3)
                JObject valueMap = (JObject) typeLanguageMap
                    [typeLanguage];
                // 2.4 )
                foreach (string item in preferredValues)
                {
                    // 2.4.1
                    if (!valueMap.ContainsKey(item))
                    {
                        continue;
                    }

                    // 2.4.2
                    return (string) valueMap[item];
                }
            }

            // 3)
            return null;
        }

        /// <summary>Retrieve container mapping.</summary>
        /// <remarks>Retrieve container mapping.</remarks>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual string GetContainer(string property)
        {
            // TODO(sblom): Do java semantics of get() on a Map return null if property is null?
            if (property == null)
            {
                return null;
            }

            if ("@graph".Equals(property))
            {
                return "@set";
            }

            if (JsonLdUtils.IsKeyword(property))
            {
                return property;
            }

            JObject td = (JObject) _termDefinitions[property
            ];
            if (td == null)
            {
                return null;
            }

            return (string) td["@container"];
        }

        public virtual bool IsReverseProperty(string property)
        {
            if (property == null)
            {
                return false;
            }

            JObject td = (JObject) _termDefinitions[property];
            if (td == null)
            {
                return false;
            }

            JToken reverse = td["@reverse"];
            return !reverse.IsNull() && (bool) reverse;
        }

        public string GetTypeMapping(string property)
        {
            if (property == null)
            {
                return null;
            }

            JToken td = _termDefinitions[property];
            if (td.IsNull())
            {
                return null;
            }

            return (string) ((JObject) td)["@type"];
        }

        public string GetLanguageMapping(string property)
        {
            if (property == null)
            {
                return null;
            }

            JObject td = (JObject) _termDefinitions[property];
            if (td == null)
            {
                return null;
            }

            return (string) td["@language"];
        }

        internal virtual JObject GetTermDefinition(string key)
        {
            return (JObject) _termDefinitions[key];
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual JToken ExpandValue(string activeProperty, JToken value)
        {
            JObject rval = new JObject();
            JObject td = GetTermDefinition(activeProperty);
            // 1)
            if (td != null && td["@type"].SafeCompare("@id"))
            {
                // TODO: i'm pretty sure value should be a string if the @type is
                // @id
                rval["@id"] = this.ExpandIri((string) value, true, false, null, null);
                return rval;
            }

            // 2)
            if (td != null && td["@type"].SafeCompare("@vocab"))
            {
                // TODO: same as above
                rval["@id"] = this.ExpandIri((string) value, true, true, null, null);
                return rval;
            }

            // 3)
            rval["@value"] = value;
            // 4)
            if (td != null && td.ContainsKey("@type"))
            {
                rval["@type"] = td["@type"];
            }
            else
            {
                // 5)
                if (value.Type == JTokenType.String)
                {
                    // 5.1)
                    if (td != null && td.ContainsKey("@language"))
                    {
                        string lang = (string) td["@language"];
                        if (lang != null)
                        {
                            rval["@language"] = lang;
                        }
                    }
                    else
                    {
                        // 5.2)
                        if (!this["@language"].IsNull())
                        {
                            rval["@language"] = this["@language"];
                        }
                    }
                }
            }

            return rval;
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual JObject GetContextValue(string activeProperty, string @string)
        {
            throw new JsonLdError(JsonLdError.Error.NotImplemented,
                "getContextValue is only used by old code so far and thus isn't implemented"
            );
        }

        public virtual JObject Serialize()
        {
            JObject ctx = new JObject();
            if (!this["@base"].IsNull() && !this["@base"].SafeCompare(_options.GetBase()))
            {
                ctx["@base"] = this["@base"];
            }

            if (!this["@language"].IsNull())
            {
                ctx["@language"] = this["@language"];
            }

            if (!this["@vocab"].IsNull())
            {
                ctx["@vocab"] = this["@vocab"];
            }

            foreach (var term in _termDefinitions.GetKeys())
            {
                var definition = (JObject) _termDefinitions[term];
                if (definition["@language"].IsNull() && definition["@container"].IsNull() && definition
                        ["@type"].IsNull() && (definition["@reverse"].IsNull() ||
                                               (definition["@reverse"].Type == JTokenType.Boolean &&
                                                (bool) definition["@reverse"] == false)))
                {
                    var cid = this.CompactIri((string) definition["@id"]);
                    ctx[term] = term.Equals(cid) ? (string) definition["@id"] : cid;
                }
                else
                {
                    var defn = new JObject();
                    var cid = this.CompactIri((string) definition["@id"]);
                    var reverseProperty = definition["@reverse"].SafeCompare(true);
                    if (!(term.Equals(cid) && !reverseProperty))
                    {
                        defn[reverseProperty ? "@reverse" : "@id"] = cid;
                    }

                    string typeMapping = (string) definition["@type"];
                    if (typeMapping != null)
                    {
                        defn["@type"] = JsonLdUtils.IsKeyword(typeMapping)
                            ? typeMapping
                            : this.CompactIri(typeMapping
                                , true);
                    }

                    if (!definition["@container"].IsNull())
                    {
                        defn["@container"] = definition["@container"];
                    }

                    JToken lang = definition["@language"];
                    if (!definition["@language"].IsNull())
                    {
                        defn["@language"] = lang.SafeCompare(false) ? null : lang;
                    }

                    ctx[term] = defn;
                }
            }

            var rval = new JObject();
            if (!ctx.IsEmpty())
            {
                rval["@context"] = ctx;
            }

            return rval;
        }
    }
}