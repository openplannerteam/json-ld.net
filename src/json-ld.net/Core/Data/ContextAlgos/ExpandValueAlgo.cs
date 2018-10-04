using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    public static class ExpandValueAlgo
    {
        public static JToken ExpandValue(this Context activeContext, string activeProperty, JToken value)
        {
            var rval = new JObject();
            var td = activeContext.GetTermDefinition(activeProperty);
            // 1)
            if (td != null && td["@type"].SafeCompare("@id"))
            {
                rval["@id"] = activeContext.ExpandIri((string) value, true, false, null, null);
                return rval;
            }


            // 2)
            if (td != null && td["@type"].SafeCompare("@vocab"))
            {
                rval["@id"] = activeContext.ExpandIri((string) value, true, true, null, null);
                return rval;
            }


            // 3)
            rval["@value"] = value;
            // 4)
            if (td != null && td.ContainsKey("@type"))
            {
                rval["@type"] = td["@type"];
                return rval;
            }

            // 5)
            if (value.Type != JTokenType.String)
            {
                return rval;
            }

            // 5.1)
            if (td != null && td.ContainsKey("@language"))
            {
                var lang = (string) td["@language"];
                if (lang != null)
                {
                    rval["@language"] = lang;
                }

                return rval;
            }

            // 5.2)
            if (!activeContext["@language"].IsNull())
            {
                rval["@language"] = activeContext["@language"];
            }

            return rval;
        }
    }
}