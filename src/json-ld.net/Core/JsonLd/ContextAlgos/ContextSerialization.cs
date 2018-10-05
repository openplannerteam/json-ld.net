using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ContextAlgos
{
    public static class ContextSerialization
    {
        public static JObject Serialize(this Context activeContext)
        {
            var ctx = new JObject();
            if (!activeContext["@base"].IsNull() && !activeContext["@base"].SafeCompare(activeContext.Options.Base))
            {
                ctx["@base"] = activeContext["@base"];
            }

            if (!activeContext["@language"].IsNull())
            {
                ctx["@language"] = activeContext["@language"];
            }

            if (!activeContext["@vocab"].IsNull())
            {
                ctx["@vocab"] = activeContext["@vocab"];
            }

            foreach (var term in activeContext.TermDefinitions.GetKeys())
            {
                var definition = (JObject) activeContext.TermDefinitions[term];
                if (definition["@language"].IsNull() 
                    && definition["@container"].IsNull()
                    && definition["@type"].IsNull() 
                    && (definition["@reverse"].IsNull() 
                    || (definition["@reverse"].IsBool()
                        && !(bool) definition["@reverse"])))
                {
                    var cid = activeContext.CompactIri((string) definition["@id"]);
                    ctx[term] = term.Equals(cid) ? (string) definition["@id"] : cid;
                }
                else
                {
                    var defn = new JObject();
                    var cid = activeContext.CompactIri((string) definition["@id"]);
                    var reverseProperty = definition["@reverse"].SafeCompare(true);
                    if (!(term.Equals(cid) && !reverseProperty))
                    {
                        defn[reverseProperty ? "@reverse" : "@id"] = cid;
                    }

                    var typeMapping = (string) definition["@type"];
                    if (typeMapping != null)
                    {
                        defn["@type"] = JsonLd.IsKeyword(typeMapping)
                            ? typeMapping
                            : activeContext.CompactIri(typeMapping
                                , true);
                    }

                    if (!definition["@container"].IsNull())
                    {
                        defn["@container"] = definition["@container"];
                    }

                    var lang = definition["@language"];
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