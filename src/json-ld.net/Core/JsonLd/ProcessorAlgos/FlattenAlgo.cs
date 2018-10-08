using System.Collections.Generic;
using JsonLD.Core.ContextAlgos;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ProcessorAlgos
{
    internal class FlattenAlgo
    {
        private readonly JsonLdOptions _opts;
        private readonly CompactionExpansionAlgo _expander;
        private readonly IDocumentLoader _loader;

        public FlattenAlgo(JsonLdOptions opts, CompactionExpansionAlgo expander,  IDocumentLoader loader)
        {
            this._opts = opts;
            this._expander = expander;
            this._loader = loader;
        }

        private JObject FlattenElement(string graphName, JObject graph, JObject defaultGraph)
        {
            // 4.1+4.2)
            JObject entry;
            if (!defaultGraph.ContainsKey(graphName))
            {
                entry = new JObject
                {
                    ["@id"] = graphName
                };
                defaultGraph[graphName] = entry;
            }
            else
            {
                entry = (JObject) defaultGraph[graphName];
            }

            // 4.3)
            // TODO: SPEC doesn't specify that this should only be added if it
            // doesn't exists
            if (!entry.ContainsKey("@graph"))
            {
                entry["@graph"] = new JArray();
            }

            var keys = new JArray(graph.GetKeys());
            keys.SortInPlace();
            foreach (string id in keys)
            {
                var node = (JObject) graph[id];
                if (!(node.ContainsKey("@id") && node.Count == 1))
                {
                    ((JArray) entry["@graph"]).Add(node);
                }
            }

            return defaultGraph;
        }

        public JToken Flatten(JToken input, JToken context)
        {
            // 2-6) NOTE: these are all the same steps as in expand
            var expanded = _expander.ExpandContext(input, _opts);
            // 7)
            if (context.IsDictContaining("@context"))
            {
                context = context["@context"];
            }
            // 8) NOTE: blank node generation variables are members of JsonLdApi
            // 9) NOTE: the next block is the Flattening Algorithm described in
            // http://json-ld.org/spec/latest/json-ld-api/#flattening-algorithm
            // 1)
            var nodeMap = new JObject
            {
                ["@default"] = new JObject()
            };
            // 2)
            new JsonLdApi(_loader).GenerateNodeMap(expanded, nodeMap);
            // 3)
            var defaultGraph = (JObject) Collections.Remove(nodeMap, "@default");
            // 4)
            foreach (var graphName in nodeMap.GetKeys())
            {
                var graph = (JObject) nodeMap[graphName];
                defaultGraph = FlattenElement(graphName, graph, defaultGraph);
            }

            // 5)
            var flattened = new JArray();
            // 6)
            var keys = new JArray(defaultGraph.GetKeys());
            keys.SortInPlace();
            foreach (string id in keys)
            {
                var node = (JObject) defaultGraph[id];
                if (!(node.ContainsKey("@id") && node.Count == 1))
                {
                    flattened.Add(node);
                }
            }

            // 8)
            if (context.IsNull() || flattened.IsEmpty())
            {
                return flattened;
            }
            
            
            var activeCtx = new Context(_opts);
            var parser = new ParsingAlgorithm(activeCtx, _loader);
            activeCtx = parser.ParseContext(context);

            
            var compacted =
                new JArray()
                {
                    _expander.CompactContext(flattened, activeCtx)
                };
      
            var rval = activeCtx.Serialize();
            rval[activeCtx.CompactIri("@graph")]
                = compacted;
            return rval;

        }
    }
}