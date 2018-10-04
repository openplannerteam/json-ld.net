using System;
using System.Collections.Generic;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    /// <summary>
    /// In JSON-LD documents, some keys and values may represent IRIs.
    /// This section defines an algorithm for transforming a string that represents an IRI into an absolute IRI or
    /// blank node identifier. It also covers transforming keyword aliases into keywords.
    /// 
    /// IRI expansion may occur during context processing or during any of the other JSON-LD algorithms.
    /// If IRI expansion occurs during context processing, then the local context and its related defined map
    /// from the Context Processing algorithm are passed to this algorithm.
    /// This allows for term definition dependencies to be processed via the Create Term Definition algorithm.
    ///
    /// 
    /// http://json-ld.org/spec/latest/json-ld-api/#iri-expansion
    /// </summary>
    public static class IriExpansionAlgorithm
    {
        public static string ExpandIri(this Context activeContext, string valueToExpand)
        {
            return activeContext.ExpandIri(valueToExpand, false, false, null, null);


        }

        public static string ExpandIri(this Context activeContext, string valueToExpand,
            bool relative, bool vocab, JObject context,
            IDictionary<string, bool> defined)
        {
            // 1)
            if (valueToExpand == null)
            {
                return null;
            }

            if (JsonLdUtils.IsKeyword(valueToExpand))
            {
                return valueToExpand;
            }


            // 2)
            if (context != null
                && context.ContainsKey(valueToExpand)
                && defined.ContainsKey(valueToExpand)
                && !defined[valueToExpand])
            {
                activeContext.CreateTermDefinition(context, valueToExpand, defined);
            }

            // 3)
            // TODO Implement case 3

            // 4)
            if (vocab && activeContext._termDefinitions.ContainsKey(valueToExpand))
            {
                var td = activeContext._termDefinitions[valueToExpand];

                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (td.Type == JTokenType.Null)
                {
                    return null;
                }

                return td["@id"].ToString();
            }

            // 5)
            var colIndex = valueToExpand.IndexOf(":", StringComparison.Ordinal);
            if (colIndex >= 0)
            {
                // 4.1)
                var prefix = valueToExpand.Substring(0, colIndex);
                var suffix = valueToExpand.Substring(colIndex + 1);

                // 4.2)
                if ("_".Equals(prefix) || suffix.StartsWith("//"))
                {
                    return valueToExpand;
                }

                // 4.3)
                if (context != null
                    && context.ContainsKey(prefix)
                    && (!defined.ContainsKey(prefix)
                        || !defined[prefix]))
                {
                    activeContext.CreateTermDefinition(context, prefix, defined);
                }

                // 4.4)
                if (activeContext._termDefinitions.ContainsKey(prefix))
                {
                    return activeContext._termDefinitions[prefix]["@id"] + suffix;
                }

                // 4.5)
                return valueToExpand;
            }

            // 5)
            if (vocab && activeContext.ContainsKey("@vocab"))
            {
                return activeContext["@vocab"].ToString() + valueToExpand;
            }

            // 6)
            if (relative)
            {
                return URL.Resolve(activeContext["@base"].ToString(), valueToExpand).ToString();
            }

            if (context != null && JsonLdUtils.IsRelativeIri(valueToExpand))
            {
                throw new JsonLdError(JsonLdError.Error.InvalidIriMapping,
                    "not an absolute IRI: " + valueToExpand);
            }

            // 7)
            return valueToExpand;
        }
    }
}