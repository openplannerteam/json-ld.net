using System;
using System.Collections;
using System.Collections.Generic;
using JsonLD.Core;
using JsonLD.Impl;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    /// <summary>http://json-ld.org/spec/latest/json-ld-api/#the-jsonldprocessor-interface
    /// 	</summary>
    public class JsonLdProcessor
    {
        
     

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static JToken Flatten(JToken input, JsonLdOptions opts)
        {
            return Flatten(input, null, opts);
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static JObject Frame(JToken input, JToken frame, JsonLdOptions
             options)
        {
            if (frame is JObject)
            {
                frame = JsonLdUtils.Clone((JObject)frame);
            }
            // TODO string/IO input
            JToken expandedInput = Expand(input, options);
            JArray expandedFrame = Expand(frame, options);
            JsonLdApi api = new JsonLdApi(expandedInput, options);
            JArray framed = api.Frame(expandedInput, expandedFrame);
            Context activeCtx = api.context.Parse(frame["@context"
                ]);
            JToken compacted = api.Compact(activeCtx, null, framed);
            if (!(compacted is JArray))
            {
                JArray tmp = new JArray();
                tmp.Add(compacted);
                compacted = tmp;
            }
            string alias = activeCtx.CompactIri("@graph");
            JObject rval = activeCtx.Serialize();
            rval[alias] = compacted;
            JsonLdUtils.RemovePreserve(activeCtx, rval, options);
            return rval;
        }

        private sealed class _Dictionary_242 : Dictionary<string, IRDFParser>
        {
            public _Dictionary_242()
            {
                {
                    // automatically register nquad serializer
                    this["application/nquads"] = new NQuadRDFParser();
                    this["text/turtle"] = new TurtleRDFParser();
                }
            }
        }

        /// <summary>
        /// a registry for RDF Parsers (in this case, JSONLDSerializers) used by
        /// fromRDF if no specific serializer is specified and options.format is set.
        /// </summary>
        /// <remarks>
        /// a registry for RDF Parsers (in this case, JSONLDSerializers) used by
        /// fromRDF if no specific serializer is specified and options.format is set.
        /// TODO: this would fit better in the document loader class
        /// </remarks>
        private static IDictionary<string, IRDFParser> rdfParsers = new _Dictionary_242();

        public static void RegisterRDFParser(string format, IRDFParser parser)
        {
            rdfParsers[format] = parser;
        }

        public static void RemoveRDFParser(string format)
        {
            JsonLD.Collections.Remove(rdfParsers, format);
        }

        /// <summary>Converts an RDF dataset to JSON-LD.</summary>
        /// <remarks>Converts an RDF dataset to JSON-LD.</remarks>
        /// <param name="dataset">
        /// a serialized string of RDF in a format specified by the format
        /// option or an RDF dataset to convert.
        /// </param>
        /// <?></?>
        /// <param name="callback">(err, output) called once the operation completes.</param>
        /// <exception cref="JsonLDNet.Core.JsonLdError"></exception>
        public static JToken FromRDF(JToken dataset, JsonLdOptions options)
        {
            // handle non specified serializer case
            IRDFParser parser = null;
            if (options.Format == null && dataset.Type == JTokenType.String)
            {
                // attempt to parse the input as nquads
                options.Format = "application/nquads";
            }
            if (rdfParsers.ContainsKey(options.Format))
            {
                parser = rdfParsers[options.Format];
            }
            else
            {
                throw new JsonLdError(JsonLdError.Error.UnknownFormat, options.Format);
            }
            // convert from RDF
            return FromRDF(dataset, options, parser);
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static JToken FromRDF(JToken dataset)
        {
            return FromRDF(dataset, new JsonLdOptions(string.Empty));
        }

        /// <summary>Uses a specific serializer.</summary>
        /// <remarks>Uses a specific serializer.</remarks>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static JToken FromRDF(JToken input, JsonLdOptions options, IRDFParser parser
            )
        {
            RDFDataset dataset = parser.Parse(input);
            // convert from RDF
            JToken rval = new JsonLdApi(options).FromRDF(dataset);
            // re-process using the generated context if outputForm is set
            if (options.OutputForm != null)
            {
                if ("expanded".Equals(options.OutputForm))
                {
                    return rval;
                }
                else
                {
                    if ("compacted".Equals(options.OutputForm))
                    {
                        return Compact(rval, dataset.GetContext(), options);
                    }
                    else
                    {
                        if ("flattened".Equals(options.OutputForm))
                        {
                            return Flatten(rval, dataset.GetContext(), options);
                        }
                        else
                        {
                            throw new JsonLdError(JsonLdError.Error.UnknownError);
                        }
                    }
                }
            }
            return rval;
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static JToken FromRDF(JToken input, IRDFParser parser)
        {
            return FromRDF(input, new JsonLdOptions(string.Empty), parser);
        }

        /// <summary>Outputs the RDF dataset found in the given JSON-LD object.</summary>
        /// <remarks>Outputs the RDF dataset found in the given JSON-LD object.</remarks>
        /// <param name="input">the JSON-LD input.</param>
        /// <param name="callback">
        /// A callback that is called when the input has been converted to
        /// Quads (null to use options.format instead).
        /// </param>
        /// <?></?>
        /// <param name="callback">(err, dataset) called once the operation completes.</param>
        /// <exception cref="JsonLDNet.Core.JsonLdError"></exception>
        public static object ToRDF(JToken input, IJSONLDTripleCallback callback, JsonLdOptions
             options)
        {
            JToken expandedInput = Expand(input, options);
            JsonLdApi api = new JsonLdApi(expandedInput, options);
            RDFDataset dataset = api.ToRDF();
            // generate namespaces from context
            if (JsonLdOptions.UseNamespaces)
            {
                JArray _input;
                if (input is JArray)
                {
                    _input = (JArray)input;
                }
                else
                {
                    _input = new JArray();
                    _input.Add((JObject)input);
                }
                foreach (JToken e in _input)
                {
                    if (((JObject)e).ContainsKey("@context"))
                    {
                        dataset.ParseContext((JObject)e["@context"]);
                    }
                }
            }
            if (callback != null)
            {
                return callback.Call(dataset);
            }
            if (options.Format != null)
            {
                if ("application/nquads".Equals(options.Format))
                {
                    return new NQuadTripleCallback().Call(dataset);
                }
                else
                {
                    if ("text/turtle".Equals(options.Format))
                    {
                        return new TurtleTripleCallback().Call(dataset);
                    }
                    else
                    {
                        throw new JsonLdError(JsonLdError.Error.UnknownFormat, options.Format);
                    }
                }
            }
            return dataset;
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static object ToRDF(JToken input, JsonLdOptions options)
        {
            return ToRDF(input, null, options);
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static object ToRDF(JToken input, IJSONLDTripleCallback callback)
        {
            return ToRDF(input, callback, new JsonLdOptions(string.Empty));
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static object ToRDF(JToken input)
        {
            return ToRDF(input, new JsonLdOptions(string.Empty));
        }


        /// <summary>Performs RDF dataset normalization on the given JSON-LD input.</summary>
        /// <remarks>
        /// Performs RDF dataset normalization on the given JSON-LD input. The output
        /// is an RDF dataset unless the 'format' option is used.
        /// </remarks>
        /// <param name="input">the JSON-LD input to normalize.</param>
        /// <?></?>
        /// <param name="callback">(err, normalized) called once the operation completes.</param>
        /// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
        /// <exception cref="JsonLDNet.Core.JsonLdError"></exception>
        public static object Normalize(JToken input, JsonLdOptions options)
        {
#if !PORTABLE
            JsonLdOptions opts = options.Clone();
            opts.Format = null;
            RDFDataset dataset = (RDFDataset)ToRDF(input, opts);
            return new JsonLdApi(options).Normalize(dataset);
#else
            throw new PlatformNotSupportedException();
#endif
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static object Normalize(JToken input)
        {
#if !PORTABLE
            return Normalize(input, new JsonLdOptions(string.Empty));
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
