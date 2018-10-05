using System;
using System.Collections.Generic;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    /// <summary>
    /// The parsing/context processing algorithm takes a flat JSON-file and parses it.
    /// When it encounters a context, the context is loaded and added to the active context.
    /// A remote context might be dereferenced.
    /// Note that the 'Parse'-method will return a clone of the active context.
    /// Also see http://json-ld.org/spec/latest/json-ld-api/#context-processing-algorithms
    /// </summary>
    public class ParsingAlgorithm
    {
        private readonly Context _activeContext;
        private readonly IDocumentLoader _downloader;

        public ParsingAlgorithm(Context activeContext, IDocumentLoader downloader)
        {
            _activeContext = activeContext;
            _downloader = downloader;
        }


        public Context ParseContext(JToken localContext, List<Uri> remoteContexts = null)
        {
            if (localContext is JArray arr)
            {
                return ParseContext(arr, remoteContexts);
            }

            return ParseContext(new JArray() {localContext}, remoteContexts);
        }

        public Context ParseContext(JArray localContext, List<Uri> remoteContexts)
        {
            // 1. Initialize result to the result of cloning active context.
            // TODO: clone?
            if (remoteContexts == null)
            {
                remoteContexts = new List<Uri>();
            }

            var result = _activeContext.Clone();

            foreach (var element in localContext)
            {
                result = HandleElement(result, remoteContexts, element);
            }

            return result;
        }


        /// <summary>
        /// Handle a single element of the context that is parsed.
        /// This _could_ change properties of result or return a new context all together, throwing away the previous context.
        /// 
        /// </summary>
        private Context HandleElement(Context result, List<Uri> remoteContexts, JToken element)
        {
            // 3.1)
            if (element.IsNull())
            {
                return new Context(_activeContext.Options);
            }

            // 3.2.3
            if (element is Context context1)
            {
                result = context1.Clone();
            }

            if (element.IsString())
            {
                return HandleStringElement(result, remoteContexts, element.ToString());
            }

            if (element is JObject dict)
            {
                return HandleDictElement(result, remoteContexts, dict);
            }

            // 3.3: The element is not a dictionary, not a string and not a previously loaded context
            // Abort
            throw new JsonLdError(JsonLdError.Error.InvalidLocalContext, element);
        }


        /// <summary>
        ///  Handle case 3.2 in the description
        /// </summary>
        private Context HandleStringElement(Context result, List<Uri> remoteContexts, string element)
        {
            // 3.2.1
            var uri = URL.Resolve(result["@base"].ToString(), element);

            // 3.2.2
            if (remoteContexts.Contains(uri))
            {
                throw new JsonLdError(JsonLdError.Error.RecursiveContextInclusion, uri);
            }

            // 3.2.3: already done - elements in the list are substituted beforehand

            remoteContexts.Add(uri);

            try
            {
                var remoteContext = _downloader.LoadDocument(uri).Document;
                if (remoteContext is JObject rContext &&
                    rContext.ContainsKey("@context"))
                {
                    // If the dereferenced document has no top-level JSON object
                    // with an @context member
                    return ParseContext(rContext["@context"], remoteContexts);
                }

                throw new JsonLdError(JsonLdError.Error.InvalidRemoteContext, element);
            }
            catch (JsonLdError err)
            {
                throw new JsonLdError(JsonLdError.Error.LoadingRemoteContextFailed, err);
            }
        }

        /// <summary>
        /// Implements 3.4 and following of the algorithm
        /// </summary>
        private static Context HandleDictElement(Context result, ICollection<Uri> remoteContexts, JObject element)
        {
            // 3.4
            if (remoteContexts.IsEmpty() && element.ContainsKey("@base"))
            {
                var value = element["@base"];
                if (value.IsNull())
                {
                    // 3.4.2
                    result.Remove("@base");
                }
                else
                {
                    if (!value.IsString())
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidBaseIri, "@base must be a string");
                    }

                    var baseVal = value.ToString();

                    if (baseVal.IsAbsoluteIri())
                    {
                        result["@base"] = baseVal;
                    }
                    else
                    {
                        var baseUri = (string) result["@base"];
                        if (!baseUri.IsAbsoluteIri())
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidBaseIri, baseUri);
                        }

                        result["@base"] = URL.Resolve(baseUri, baseVal);
                    }
                }
            }

            // 3.5
            if (element.ContainsKey("@version"))
            {
               // TODO check version
            }


            // 3.6
            if (element.ContainsKey("@vocab"))
            {
                var value = element["@vocab"];
                if (value.IsNull())
                {
                    Collections.Remove(result, "@vocab");
                }
                else
                {
                    if (value.IsString())
                    {
                        if (!((string) value).IsAbsoluteIri())
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidVocabMapping,
                                "@value must be an absolute IRI"
                            );
                        }

                        result["@vocab"] = value;
                    }
                    else
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidVocabMapping,
                            "@vocab must be a string or null"
                        );
                    }
                }
            }

            // 3.7
            if (element.ContainsKey("@language"))
            {
                var value = element["@language"];
                if (value.IsNull())
                {
                    Collections.Remove(result, "@language");
                }
                else
                {
                    if (value.IsString())
                    {
                        result["@language"] = value.ToString().ToLower();
                    }
                    else
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidDefaultLanguage, value);
                    }
                }
            }

            // 3.8
            IDictionary<string, bool> defined = new Dictionary<string, bool>();
            foreach (var key in element.GetKeys())
            {
                if ("@base".Equals(key) || "@vocab".Equals(key) || "@language".Equals(key))
                {
                    continue;
                }

                result.CreateTermDefinition(element, key, defined);
            }

            return result;
        }
    }
}