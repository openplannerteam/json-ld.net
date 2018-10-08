using System.Runtime.InteropServices.ComTypes;
using System.Xml;
using JsonLD.Core.ContextAlgos;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ProcessorAlgos
{
    internal class FrameAlgo
    {
        private readonly JsonLdOptions _options;
        private readonly CompactionExpansionAlgo _expander;
        private readonly IDocumentLoader _loader;


        public FrameAlgo(JsonLdOptions options, CompactionExpansionAlgo expander, IDocumentLoader loader)
        {
            _options = options;
            _expander = expander;
            _loader = loader;
        }
      
        public JObject Frame(JToken input, JToken frame)
        {
            if (frame is JObject)
            {
                frame = ((JObject) frame).DeepClone();
            }

            var expandedInput = _expander.ExpandContext(input, _options);
            var expandedFrame = _expander.ExpandContext(frame, _options);
            var api = new JsonLdApi(_options, _loader);
            var framed = api.Frame(expandedInput, expandedFrame);

            var parser = new ParsingAlgorithm(api.context, _loader);
            var activeCtx = parser.ParseContext(frame["@context"]);

            var compacted = _expander.Compact(activeCtx, null, framed, _options.CompactArrays);
            // (activeCtx, null, framed);
            if (!(compacted is JArray))
            {
                var tmp = new JArray {compacted};
                compacted = tmp;
            }

            var rval = activeCtx.Serialize();
            rval[activeCtx.CompactIri("@graph")] = compacted;
            RemovePreserve(activeCtx, rval);
            return rval;
        }

        /// <summary>Removes the @preserve keywords as the last step of the framing algorithm.
        /// 	</summary>
        /// <param name="ctx">the active context used to compact the input.</param>
        private JToken RemovePreserve(Context ctx, JToken input)
        {
            // recurse through arrays
            if (input.IsArray())
            {
                var output = new JArray();
                foreach (var i in (JArray) input)
                {
                    var result = RemovePreserve(ctx, i);
                    // drop nulls from arrays
                    if (!result.IsNull())
                    {
                        output.Add(result);
                    }
                }

                return output;
            }


            if (!input.IsObject())
            {
                return input;
            }


            // remove @preserve
            if (input.IsDictContaining("@preserve", out var dict))
            {
                return dict["@preserve"].SafeCompare("@null") ? null : dict["@preserve"];
            }

            // skip @values
            if (input.IsValue())
            {
                return input;
            }

            // recurse through @lists
            if (input.IsList())
            {
                ((JObject) input)["@list"] =
                    RemovePreserve(ctx, ((JObject) input)["@list"]);
                return input;
            }

            // recurse through properties
            foreach (var prop in input.GetKeys())
            {
                var result = RemovePreserve(ctx, ((JObject) input)[prop]);
                var container = ctx.GetContainer(prop);
                if (_options.CompactArrays
                    && result.IsArray()
                    && ((JArray) result).Count == 1
                    && container == null)
                {
                    result = ((JArray) result)[0];
                }

                ((JObject) input)[prop] = result;
            }

            return input;
        }
    }
}