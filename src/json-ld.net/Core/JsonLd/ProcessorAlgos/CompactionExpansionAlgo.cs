using System;
using System.Collections.Generic;
using JsonLD.Core.ContextAlgos;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ProcessorAlgos
{
    /// <summary>
    /// Compaction and expansion algorithm of an entire JSON-LD
    /// </summary>
    public class CompactionExpansionAlgo
    {
        private readonly IDocumentLoader loader;
        private readonly JsonLdOptions opts;

        public CompactionExpansionAlgo(IDocumentLoader loader, JsonLdOptions opts)
        {
            this.loader = loader;
            this.opts = opts;
        }

        public JObject Compact(JToken input, JToken context)
        {
            // 1)
            // 2-6) NOTE: these are all the same steps as in expand
            JToken expanded = Expand(input, opts);
            // 7)
            if (context.IsDictContaining("@context", out var dct))
            {
                context = dct["@context"];
            }


            var activeCtx = new Context(opts);
            var parsingAlgo = new ParsingAlgorithm(activeCtx, loader);
            activeCtx = parsingAlgo.ParseContext(context);

            // 8)
            var compacted = new JsonLdApi(loader).Compact(activeCtx, null, expanded, opts.CompactArrays);
            // final step of Compaction Algorithm
            // TODO: SPEC: the result result is a NON EMPTY array,
            if (compacted is JArray arr)
            {
                if (arr.IsEmpty())
                {
                    compacted = new JObject();
                }
                else
                {
                    var tmp = new JObject
                        // TODO: SPEC: doesn't specify to use vocab = true here
                        {
                            [activeCtx.CompactIri("@graph", true)] = compacted
                        };
                    compacted = tmp;
                }
            }

            if (compacted.IsNull() || context.IsNull())
            {
                return (JObject) compacted;
            }


            if (context is JObject o && !o.IsEmpty()
                || context is JArray array && !array.IsEmpty())
            {
                compacted["@context"] = context;
            }

            // 9)
            return (JObject) compacted;
        }


        public JArray Expand(JToken input, JsonLdOptions opts)
        {
            // 2) verification of DOMString IRI
            var isIriString = input.IsString();
            if (isIriString)
            {
                var hasColon = false;
                foreach (var c in (string) input)
                {
                    if (c == ':')
                    {
                        hasColon = true;
                    }

                    if (hasColon || (c != '{' && c != '[')) continue;
                    isIriString = false;
                    break;
                }
            }

            if (isIriString)
            {
                var tmp = opts.documentLoader.LoadDocument(new Uri((string) input));
                input = tmp.document;

                // if set, the base in the LD-options overrides the base iri in the active context.
                // Thus we only set this as the base iri if it's not already set in
                // options
                if (opts.Base == null)
                {
                    opts.Base = (string) input;
                }
            }


            // 3)
            var activeCtx = new Context(opts);
            // 4)
            if (opts.ExpandContext != null)
            {
                var exCtx = opts.ExpandContext;
                if (exCtx.IsDictContaining("@context", out var ctxDct))
                {
                    exCtx = (JObject) ctxDct["@context"];
                }

                var parser = new ParsingAlgorithm(activeCtx, loader);
                activeCtx = parser.ParseContext(exCtx);
            }

            // 5)
            // TODO: add support for getting a context from HTTP when content-type is set to a jsonld compatable format
            // 6)
            var expanded = new JsonLdApi(loader).Expand(activeCtx, input);
            // final step of Expansion Algorithm
            if (expanded.IsDictContaining("@graph", out var graph)
                && graph.Count == 1)
            {
                expanded = graph["@graph"];
            }
            else if (expanded.IsNull())
            {
                expanded = new JArray();
            }

            if (expanded is JArray array)
            {
                return array;
            }

            // normalize to an array
            return new JArray {expanded};
        }
    }
}