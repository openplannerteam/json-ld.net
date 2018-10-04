using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using JsonLD.Core.ContextAlgos;

namespace JsonLD.Core.ContextAlgos
{
    /// <summary>
    /// This algorithm compacts a JSON-LD document, such that the given context is applied.
    /// This must result in shortening
    ///  - any applicable IRIs to terms or compact IRIs
    ///  - any applicable keywords to keyword aliases
    ///  - and any applicable JSON-LD values expressed in expanded form to simple values such as strings or numbers.
    ///
    /// See https://json-ld.org/spec/latest/json-ld-api/#compaction-algorithm
    /// </summary>
    public static class ValueCompactionAlgorithm
    {
        public static JToken CompactValue(this Context context, string activeProperty, JObject value)
        {
            var dict = (IDictionary<string, JToken>) value;
            var numberMembers = value.Count;
            if (dict.ContainsKey("@index") && "@index".Equals(context.GetContainer(activeProperty)))
            {
                numberMembers--;
            }

            if (numberMembers > 2)
            {
                return value;
            }

            var typeMapping = context.GetTypeMapping(activeProperty);
            var languageMapping = context.GetLanguageMapping(activeProperty);
            if (dict.ContainsKey("@id"))
            {
                if (numberMembers == 1 && "@id".Equals(typeMapping))
                {
                    return context.CompactIri((string) value["@id"]);
                }

                if (numberMembers == 1 && "@vocab".Equals(typeMapping))
                {
                    return context.CompactIri((string) value["@id"], true);
                }

                return value;
            }

            var valueValue = value["@value"];
            
            if (dict.ContainsKey("@type") && value["@type"].SafeCompare(typeMapping))
            {
                return valueValue;
            }

            if (dict.ContainsKey("@language"))
            {
                // TODO: SPEC: doesn't specify to check default language as well
                if (value["@language"].SafeCompare(languageMapping) ||
                    value["@language"].SafeCompare(context["@language"]))
                {
                    return valueValue;
                }
            }

            if (numberMembers == 1 
               && (
                    valueValue.Type != JTokenType.String
                    || !((IDictionary<string, JToken>) context).ContainsKey("@language") 
                    || (context.GetTermDefinition(activeProperty).ContainsKey("@language") 
                        && languageMapping == null )))
            {
                return valueValue;
            }

            return value;
        }
    }
}