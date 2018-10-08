using System.Collections.Generic;
using JsonLD.Util;
using Newtonsoft.Json.Linq;
using JsonLD.Core.ContextAlgos;

namespace JsonLD.Core
{
    public class JsonLdApi
    {
        internal JsonLdOptions opts;

        internal JToken value = null;

        internal Context context = null;

        private readonly IDocumentLoader downloader;

        public JsonLdApi(IDocumentLoader downloader)
        {
            this.downloader = downloader;
            opts = new JsonLdOptions(string.Empty);
        }

        public JsonLdApi(JsonLdOptions opts, IDocumentLoader downloader)
        {
            this.downloader = downloader;
            this.opts = opts ?? new JsonLdOptions(string.Empty);
        }

      
        internal virtual void GenerateNodeMap(JToken element, JObject nodeMap)
        {
            GenerateNodeMap(element, nodeMap, "@default", null, null, null);
        }

        internal virtual void GenerateNodeMap(JToken element, JObject
            nodeMap, string activeGraph, JToken activeSubject, string activeProperty, JObject list)
        {
            GenerateNodeMap(element, nodeMap, activeGraph, activeSubject, activeProperty, list,
                skipSetContainsCheck: false);
        }

        private void GenerateNodeMap(JToken element, JObject nodeMap,
            string activeGraph, JToken activeSubject, string activeProperty, JObject list, bool skipSetContainsCheck)
        {
            // 1)
            if (element is JArray)
            {
                JsonLdSet set = null;

                if (list == null)
                {
                    set = new JsonLdSet();
                }

                // 1.1)
                foreach (JToken item in (JArray) element)
                {
                    skipSetContainsCheck = false;

                    if (set != null)
                    {
                        skipSetContainsCheck = set.Add(item);
                    }

                    GenerateNodeMap(item, nodeMap, activeGraph, activeSubject, activeProperty, list,
                        skipSetContainsCheck);
                }

                return;
            }

            // for convenience
            IDictionary<string, JToken> elem = (IDictionary<string, JToken>) element;
            // 2)
            if (!((IDictionary<string, JToken>) nodeMap).ContainsKey(activeGraph))
            {
                nodeMap[activeGraph] = new JObject();
            }

            JObject graph = (JObject) nodeMap[activeGraph
            ];
            JObject node = (JObject) ((activeSubject.IsNull() || activeSubject.Type != JTokenType.String)
                ? null
                : graph[(string) activeSubject]);
            // 3)
            if (elem.ContainsKey("@type"))
            {
                // 3.1)
                JArray oldTypes;
                JArray newTypes = new JArray();
                if (elem["@type"] is JArray)
                {
                    oldTypes = (JArray) elem["@type"];
                }
                else
                {
                    oldTypes = new JArray();
                    oldTypes.Add((string) elem["@type"]);
                }

                foreach (string item in oldTypes)
                {
                    if (item.StartsWith("_:"))
                    {
                        newTypes.Add(GenerateBlankNodeIdentifier(item));
                    }
                    else
                    {
                        newTypes.Add(item);
                    }
                }

                if (elem["@type"] is JArray)
                {
                    elem["@type"] = newTypes;
                }
                else
                {
                    elem["@type"] = newTypes[0];
                }
            }

            // 4)
            if (elem.ContainsKey("@value"))
            {
                // 4.1)
                if (list == null)
                {
                    JsonLdUtils.MergeValue(node, activeProperty, (JObject) elem);
                }
                else
                {
                    // 4.2)
                    JsonLdUtils.MergeValue(list, "@list", (JObject) elem);
                }
            }
            else
            {
                // 5)
                if (elem.ContainsKey("@list"))
                {
                    // 5.1)
                    JObject result = new JObject();
                    result["@list"] = new JArray();
                    // 5.2)
                    //for (final Object item : (List<Object>) elem.get("@list")) {
                    //    generateNodeMap(item, nodeMap, activeGraph, activeSubject, activeProperty, result);
                    //}
                    GenerateNodeMap(elem["@list"], nodeMap, activeGraph, activeSubject, activeProperty
                        , result);
                    // 5.3)
                    JsonLdUtils.MergeValue(node, activeProperty, result);
                }
                else
                {
                    // 6)
                    // 6.1)
                    string id = (string) JsonLD.Collections.Remove(elem, "@id");
                    if (id != null)
                    {
                        if (id.StartsWith("_:"))
                        {
                            id = GenerateBlankNodeIdentifier(id);
                        }
                    }
                    else
                    {
                        // 6.2)
                        id = GenerateBlankNodeIdentifier(null);
                    }

                    // 6.3)
                    if (!graph.ContainsKey(id))
                    {
                        JObject tmp = new JObject();
                        tmp["@id"] = id;
                        graph[id] = tmp;
                    }

                    // 6.4) TODO: SPEC this line is asked for by the spec, but it breaks various tests
                    //node = (Map<String, Object>) graph.get(id);
                    // 6.5)
                    if (activeSubject is JObject)
                    {
                        // 6.5.1)
                        JsonLdUtils.MergeValue((JObject) graph[id], activeProperty, activeSubject
                        );
                    }
                    else
                    {
                        // 6.6)
                        if (activeProperty != null)
                        {
                            JObject reference = new JObject();
                            reference["@id"] = id;
                            // 6.6.2)
                            if (list == null)
                            {
                                // 6.6.2.1+2)
                                JsonLdUtils.MergeValue(node, activeProperty, reference, skipSetContainsCheck);
                            }
                            else
                            {
                                // 6.6.3) TODO: SPEC says to add ELEMENT to @list member, should
                                // be REFERENCE
                                JsonLdUtils.MergeValue(list, "@list", reference);
                            }
                        }
                    }

                    // TODO: SPEC this is removed in the spec now, but it's still needed (see 6.4)
                    node = (JObject) graph[id];
                    // 6.7)
                    if (elem.ContainsKey("@type"))
                    {
                        foreach (JToken type in (JArray) JsonLD.Collections.Remove(elem, "@type"
                        ))
                        {
                            JsonLdUtils.MergeValue(node, "@type", type);
                        }
                    }

                    // 6.8)
                    if (elem.ContainsKey("@index"))
                    {
                        JToken elemIndex = JsonLD.Collections.Remove(elem, "@index");
                        if (node.ContainsKey("@index"))
                        {
                            if (!JsonLdUtils.DeepCompare(node["@index"], elemIndex))
                            {
                                throw new JsonLdError(JsonLdError.Error.ConflictingIndexes);
                            }
                        }
                        else
                        {
                            node["@index"] = elemIndex;
                        }
                    }

                    // 6.9)
                    if (elem.ContainsKey("@reverse"))
                    {
                        // 6.9.1)
                        JObject referencedNode = new JObject();
                        referencedNode["@id"] = id;
                        // 6.9.2+6.9.4)
                        JObject reverseMap = (JObject) JsonLD.Collections.Remove
                            (elem, "@reverse");
                        // 6.9.3)
                        foreach (string property in reverseMap.GetKeys())
                        {
                            JArray values = (JArray) reverseMap[property];
                            // 6.9.3.1)
                            foreach (JToken value in values)
                            {
                                // 6.9.3.1.1)
                                GenerateNodeMap(value, nodeMap, activeGraph, referencedNode, property, null);
                            }
                        }
                    }

                    // 6.10)
                    if (elem.ContainsKey("@graph"))
                    {
                        GenerateNodeMap(JsonLD.Collections.Remove(elem, "@graph"), nodeMap, id, null,
                            null, null);
                    }

                    // 6.11)
                    JArray keys = new JArray(element.GetKeys());
                    keys.SortInPlace();
                    foreach (string property_1 in keys)
                    {
                        var eachProperty_1 = property_1;
                        JToken value = elem[eachProperty_1];
                        // 6.11.1)
                        if (eachProperty_1.StartsWith("_:"))
                        {
                            eachProperty_1 = GenerateBlankNodeIdentifier(eachProperty_1);
                        }

                        // 6.11.2)
                        if (!node.ContainsKey(eachProperty_1))
                        {
                            node[eachProperty_1] = new JArray();
                        }

                        // 6.11.3)
                        GenerateNodeMap(value, nodeMap, activeGraph, id, eachProperty_1, null);
                    }
                }
            }
        }

        private readonly JObject blankNodeIdentifierMap = new JObject();

        private int blankNodeCounter = 0;

        internal virtual string GenerateBlankNodeIdentifier(string id)
        {
            if (id != null && blankNodeIdentifierMap.ContainsKey(id))
            {
                return (string) blankNodeIdentifierMap[id];
            }

            string bnid = "_:b" + blankNodeCounter++;
            if (id != null)
            {
                blankNodeIdentifierMap[id] = bnid;
            }

            return bnid;
        }

        internal virtual string GenerateBlankNodeIdentifier()
        {
            return GenerateBlankNodeIdentifier(null);
        }

        private class FramingContext
        {
            public bool embed;

            public bool @explicit;

            public bool omitDefault;

            public FramingContext()
            {
                embed = true;
                @explicit = false;
                omitDefault = false;
                embeds = null;
            }

            public IDictionary<string, EmbedNode> embeds = null;
        }

        private class EmbedNode
        {
            public JToken parent = null;

            public string property = null;

            internal EmbedNode(JsonLdApi _enclosing)
            {
                this._enclosing = _enclosing;
            }

            private readonly JsonLdApi _enclosing;
        }

        private JObject nodeMap;

        /// <summary>Performs JSON-LD framing.</summary>
        /// <remarks>Performs JSON-LD framing.</remarks>
        /// <param name="input">the expanded JSON-LD to frame.</param>
        /// <param name="frame">the expanded JSON-LD frame to use.</param>
        /// <param name="options">the framing options.</param>
        /// <returns>the framed output.</returns>
        /// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual JArray Frame(JToken input, JArray frame)
        {
            // create framing state
            var state = new FramingContext();
            if (this.opts.Embed != null)
            {
                state.embed = opts.Embed.Value;
            }

            if (this.opts.Explicit != null)
            {
                state.@explicit = opts.Explicit.Value;
            }

            if (this.opts.OmitDefault != null)
            {
                state.omitDefault = opts.OmitDefault.Value;
            }

            // use tree map so keys are sotred by default
            // XXX BUG BUG BUG XXX (sblom) Figure out where this needs to be sorted and use extension methods to return sorted enumerators or something!
            JObject nodes = new JObject();

            GenerateNodeMap(input, nodes);
            this.nodeMap = (JObject) nodes["@default"];

            JArray framed = new JArray();

            // NOTE: frame validation is done by the function not allowing anything
            // other than list to me passed
            Frame(state, this.nodeMap, (frame != null && frame.Count > 0 ? (JObject) frame[0] : new JObject()), framed,
                null);
            return framed;
        }

        /// <summary>Frames subjects according to the given frame.</summary>
        /// <remarks>Frames subjects according to the given frame.</remarks>
        /// <param name="state">the current framing state.</param>
        /// <param name="subjects">the subjects to filter.</param>
        /// <param name="frame">the frame.</param>
        /// <param name="parent">the parent subject or top-level array.</param>
        /// <param name="property">the parent property, initialized to null.</param>
        /// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private void Frame(JsonLdApi.FramingContext state, JObject nodes
            , JObject frame, JToken parent, string property)
        {
            // filter out subjects that match the frame
            JObject matches = FilterNodes(state, nodes, frame);

            // get flags for current frame
            bool embedOn = GetFrameFlag(frame, "@embed", state.embed);
            bool explicitOn = GetFrameFlag(frame, "@explicit", state.@explicit);

            // add matches to output
            JArray ids = new JArray(matches.GetKeys());
            ids.SortInPlace();
            foreach (string id in ids)
            {
                if (property == null)
                {
                    state.embeds = new Dictionary<string, JsonLdApi.EmbedNode>();
                }

                // start output
                JObject output = new JObject();
                output["@id"] = id;
                // prepare embed meta info
                JsonLdApi.EmbedNode embeddedNode = new JsonLdApi.EmbedNode(this);
                embeddedNode.parent = parent;
                embeddedNode.property = property;
                // if embed is on and there is an existing embed
                if (embedOn && state.embeds.ContainsKey(id))
                {
                    JsonLdApi.EmbedNode existing = state.embeds[id];
                    embedOn = false;
                    if (existing.parent is JArray)
                    {
                        foreach (JToken p in (JArray) (existing.parent))
                        {
                            if (JsonLdUtils.CompareValues(output, p))
                            {
                                embedOn = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // existing embed's parent is an object
                        if (((JObject) existing.parent).ContainsKey(existing.property))
                        {
                            foreach (JToken v in (JArray) ((JObject) existing.parent)[existing.property])
                            {
                                if (v is JObject && ((JObject) v)["@id"].SafeCompare(id))
                                {
                                    embedOn = true;
                                    break;
                                }
                            }
                        }
                    }

                    // existing embed has already been added, so allow an overwrite
                    if (embedOn)
                    {
                        RemoveEmbed(state, id);
                    }
                }

                // not embedding, add output without any other properties
                if (!embedOn)
                {
                    AddFrameOutput(state, parent, property, output);
                }
                else
                {
                    // add embed meta info
                    state.embeds[id] = embeddedNode;
                    // iterate over subject properties
                    JObject element = (JObject) matches[id];
                    JArray props = new JArray(element.GetKeys());
                    props.SortInPlace();
                    foreach (string prop in props)
                    {
                        // copy keywords to output
                        if (JsonLd.IsKeyword(prop))
                        {
                            output[prop] = element[prop].DeepClone();
                            continue;
                        }

                        // if property isn't in the frame
                        if (!frame.ContainsKey(prop))
                        {
                            // if explicit is off, embed values
                            if (!explicitOn)
                            {
                                EmbedValues(state, element, prop, output);
                            }

                            continue;
                        }

                        // add objects
                        JArray value = (JArray) element[prop];
                        foreach (JToken item in value)
                        {
                            // recurse into list
                            if ((item is JObject) && ((JObject) item).ContainsKey("@list"))
                            {
                                // add empty list
                                JObject list = new JObject();
                                list["@list"] = new JArray();
                                AddFrameOutput(state, output, prop, list);
                                // add list objects
                                foreach (JToken listitem in (JArray) ((JObject) item)["@list"
                                ])
                                {
                                    // recurse into subject reference
                                    if (JsonLdUtils.IsNodeReference(listitem))
                                    {
                                        JObject tmp = new JObject();
                                        string itemid = (string) ((IDictionary<string, JToken>) listitem)["@id"];
                                        // TODO: nodes may need to be node_map,
                                        // which is global
                                        tmp[itemid] = this.nodeMap[itemid];
                                        Frame(state, tmp, (JObject) ((JArray) frame[prop])[0], list
                                            , "@list");
                                    }
                                    else
                                    {
                                        // include other values automatcially (TODO:
                                        // may need JsonLdUtils.clone(n))
                                        AddFrameOutput(state, list, "@list", listitem);
                                    }
                                }
                            }
                            else
                            {
                                // recurse into subject reference
                                if (JsonLdUtils.IsNodeReference(item))
                                {
                                    JObject tmp = new JObject();
                                    string itemid = (string) ((JObject) item)["@id"];
                                    // TODO: nodes may need to be node_map, which is
                                    // global
                                    tmp[itemid] = this.nodeMap[itemid];
                                    Frame(state, tmp, (JObject) ((JArray) frame[prop])[0], output
                                        , prop);
                                }
                                else
                                {
                                    // include other values automatically (TODO: may
                                    // need JsonLdUtils.clone(o))
                                    AddFrameOutput(state, output, prop, item);
                                }
                            }
                        }
                    }

                    // handle defaults
                    props = new JArray(frame.GetKeys());
                    props.SortInPlace();
                    foreach (string prop_1 in props)
                    {
                        // skip keywords
                        if (JsonLd.IsKeyword(prop_1))
                        {
                            continue;
                        }

                        JArray pf = (JArray) frame[prop_1];
                        JObject propertyFrame = pf.Count > 0 ? (JObject) pf[0] : null;
                        if (propertyFrame == null)
                        {
                            propertyFrame = new JObject();
                        }

                        bool omitDefaultOn = GetFrameFlag(propertyFrame, "@omitDefault", state.omitDefault
                        );
                        if (!omitDefaultOn && !output.ContainsKey(prop_1))
                        {
                            JToken def = "@null";
                            if (propertyFrame.ContainsKey("@default"))
                            {
                                def = propertyFrame["@default"].DeepClone();
                            }

                            if (!(def is JArray))
                            {
                                JArray tmp = new JArray();
                                tmp.Add(def);
                                def = tmp;
                            }

                            JObject tmp1 = new JObject();
                            tmp1["@preserve"] = def;
                            JArray tmp2 = new JArray();
                            tmp2.Add(tmp1);
                            output[prop_1] = tmp2;
                        }
                    }

                    // add output to parent
                    AddFrameOutput(state, parent, property, output);
                }
            }
        }

        private bool GetFrameFlag(JObject frame, string name, bool thedefault
        )
        {
            JToken value = frame[name];
            if (value is JArray)
            {
                if (((JArray) value).Count > 0)
                {
                    value = ((JArray) value)[0];
                }
            }

            if (value is JObject && ((JObject) value).ContainsKey("@value"
                ))
            {
                value = ((JObject) value)["@value"];
            }

            if (value != null && value.Type == JTokenType.Boolean)
            {
                return (bool) value;
            }

            return thedefault;
        }

        /// <summary>Removes an existing embed.</summary>
        /// <remarks>Removes an existing embed.</remarks>
        /// <param name="state">the current framing state.</param>
        /// <param name="id">the @id of the embed to remove.</param>
        private static void RemoveEmbed(JsonLdApi.FramingContext state, string id)
        {
// get existing embed
            IDictionary<string, JsonLdApi.EmbedNode> embeds = state.embeds;
            JsonLdApi.EmbedNode embed = embeds[id];
            JToken parent = embed.parent;
            string property = embed.property;
// create reference to replace embed
            JObject node = new JObject();
            node["@id"] = id;
// remove existing embed
            if (JsonLdUtils.IsNode(parent))
            {
// replace subject with reference
                JArray newvals = new JArray();
                JArray oldvals = (JArray) ((JObject) parent)[property
                ];
                foreach (JToken v in oldvals)
                {
                    if (v is JObject && ((JObject) v)["@id"].SafeCompare(id))
                    {
                        newvals.Add(node);
                    }
                    else
                    {
                        newvals.Add(v);
                    }
                }

                ((JObject) parent)[property] = newvals;
            }

// recursively remove dependent dangling embeds
            RemoveDependents(embeds, id);
        }

        private static void RemoveDependents(IDictionary<string, JsonLdApi.EmbedNode> embeds
            , string id)
        {
// get embed keys as a separate array to enable deleting keys in map
            List<string> embedsKeys = new List<string>(embeds.Keys);
            foreach (string id_dep in embedsKeys)
            {
                JsonLdApi.EmbedNode e;
                if (!embeds.TryGetValue(id_dep, out e))
                {
                    continue;
                }

                JToken p = !e.parent.IsNull() ? e.parent : new JObject();
                if (!(p is JObject))
                {
                    continue;
                }

                string pid = (string) ((JObject) p)["@id"];
                if (Obj.Equals(id, pid))
                {
                    JsonLD.Collections.Remove(embeds, id_dep);
                    RemoveDependents(embeds, id_dep);
                }
            }
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private JObject FilterNodes(JsonLdApi.FramingContext state, JObject nodes, JObject frame)
        {
            JObject rval = new JObject();
            foreach (string id in nodes.GetKeys())
            {
                JObject element = (JObject) nodes[id];
                if (element != null && FilterNode(state, element, frame))
                {
                    rval[id] = element;
                }
            }

            return rval;
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private bool FilterNode(JsonLdApi.FramingContext state, JObject node, JObject frame)
        {
            JToken types = frame["@type"];
            if (!types.IsNull())
            {
                if (!(types is JArray))
                {
                    throw new JsonLdError(JsonLdError.Error.SyntaxError, "frame @type must be an array");
                }

                var nodeTypes = node["@type"];
                if (nodeTypes.IsNull())
                {
                    nodeTypes = new JArray();
                }
                else if (!(nodeTypes is JArray))
                {
                    throw new JsonLdError(JsonLdError.Error.SyntaxError, "node @type must be an array"
                    );
                }

                if (((JArray) types).Count == 1 && ((JArray) types)[0] is JObject
                                                && ((JObject) ((JArray) types)[0]).Count == 0)
                {
                    return !((JArray) nodeTypes).IsEmpty();
                }

                foreach (JToken i in (JArray) nodeTypes)
                {
                    foreach (JToken j in (JArray) types)
                    {
                        if (JsonLdUtils.DeepCompare(i, j))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            foreach (string key in frame.GetKeys())
            {
                if ("@id".Equals(key) || !JsonLd.IsKeyword(key) && !(node.ContainsKey(key)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Adds framing output to the given parent.</summary>
        /// <remarks>Adds framing output to the given parent.</remarks>
        /// <param name="state">the current framing state.</param>
        /// <param name="parent">the parent to add to.</param>
        /// <param name="property">the parent property.</param>
        /// <param name="output">the output to add.</param>
        private static void AddFrameOutput(JsonLdApi.FramingContext state, JToken parent,
            string property, JToken output)
        {
            if (parent is JObject)
            {
                JArray prop = (JArray) ((JObject) parent)[property];
                if (prop == null)
                {
                    prop = new JArray();
                    ((JObject) parent)[property] = prop;
                }

                prop.Add(output);
            }
            else
            {
                ((JArray) parent).Add(output);
            }
        }

        /// <summary>
        /// Embeds values for the given subject and property into the given output
        /// during the framing algorithm.
        /// </summary>
        /// <remarks>
        /// Embeds values for the given subject and property into the given output
        /// during the framing algorithm.
        /// </remarks>
        /// <param name="state">the current framing state.</param>
        /// <param name="element">the subject.</param>
        /// <param name="property">the property.</param>
        /// <param name="output">the output.</param>
        private void EmbedValues(JsonLdApi.FramingContext state, JObject element, string property, JToken output)
        {
// embed subject properties in output
            JArray objects = (JArray) element[property];
            foreach (JToken o in objects)
            {
                var eachObj = o;
                if (eachObj is JObject && ((JObject) eachObj).ContainsKey("@list"))
                {
                    JObject list = new JObject {{"@list", new JArray()}};
                    if (output is JArray)
                    {
                        ((JArray) output).Add(list);
                    }
                    else
                    {
                        output[property] = new JArray(list);
                    }

                    EmbedValues(state, (JObject) eachObj, "@list", list["@list"]);
                }
// handle subject reference
                else if (JsonLdUtils.IsNodeReference(eachObj))
                {
                    string sid = (string) ((JObject) eachObj)["@id"];
// embed full subject if isn't already embedded
                    if (!state.embeds.ContainsKey(sid))
                    {
// add embed
                        JsonLdApi.EmbedNode embed = new JsonLdApi.EmbedNode(this);
                        embed.parent = output;
                        embed.property = property;
                        state.embeds[sid] = embed;
// recurse into subject
                        eachObj = new JObject();
                        JObject s = (JObject) this.nodeMap[sid];
                        if (s == null)
                        {
                            s = new JObject();
                            s["@id"] = sid;
                        }

                        foreach (string prop in s.GetKeys())
                        {
// copy keywords
                            if (JsonLd.IsKeyword(prop))
                            {
                                ((JObject) eachObj)[prop] = s[prop].DeepClone();
                                continue;
                            }

                            EmbedValues(state, s, prop, eachObj);
                        }
                    }

                    AddFrameOutput(state, output, property, eachObj);
                }
                else
                {
// copy non-subject value
                    AddFrameOutput(state, output, property, eachObj.DeepClone());
                }
            }
        }

        /// <summary>Helper class for node usages</summary>
        /// <author>tristan</author>
        private class UsagesNode
        {
            public UsagesNode(JsonLdApi _enclosing, JsonLdApi.NodeMapNode node, string property
                , JObject value)
            {
                this._enclosing = _enclosing;
                this.node = node;
                this.property = property;
                this.value = value;
            }

            public JsonLdApi.NodeMapNode node = null;
            public string property = null;
            public JObject value = null;
            private readonly JsonLdApi _enclosing;
        }

//[System.Serializable]
        private class NodeMapNode : JObject
        {
            public IList<UsagesNode> usages = new List<UsagesNode>();

            public NodeMapNode(JsonLdApi _enclosing, string id) : base()
            {
                this._enclosing = _enclosing;
                this["@id"] = id;
            }

// helper fucntion for 4.3.3
            public virtual bool IsWellFormedListNode()
            {
                if (this.usages.Count != 1)
                {
                    return false;
                }

                int keys = 0;
                if (this.ContainsKey(JsonldConsts.RdfFirst))
                {
                    keys++;
                    if (!(this[JsonldConsts.RdfFirst] is JArray && ((JArray) this[JsonldConsts.RdfFirst
                          ]).Count == 1))
                    {
                        return false;
                    }
                }

                if (this.ContainsKey(JsonldConsts.RdfRest))
                {
                    keys++;
                    if (!(this[JsonldConsts.RdfRest] is JArray && ((JArray) this[JsonldConsts.RdfRest
                          ]).Count == 1))
                    {
                        return false;
                    }
                }

                if (this.ContainsKey("@type"))
                {
                    keys++;
                    if (!(this["@type"] is JArray && ((JArray) this["@type"]).Count == 1) && JsonldConsts
                            .RdfList.Equals(((JArray) this["@type"])[0]))
                    {
                        return false;
                    }
                }

// TODO: SPEC: 4.3.3 has no mention of @id
                if (this.ContainsKey("@id"))
                {
                    keys++;
                }

                if (keys < Count)
                {
                    return false;
                }

                return true;
            }

// return this node without the usages variable
            public virtual JObject Serialize()
            {
                return new JObject(this);
            }

            private readonly JsonLdApi _enclosing;
        }

        /// <summary>Converts RDF statements into JSON-LD.</summary>
        /// <remarks>Converts RDF statements into JSON-LD.</remarks>
        /// <param name="statements">the RDF statements.</param>
        /// <param name="options">the RDF conversion options.</param>
        /// <param name="callback">(err, output) called once the operation completes.</param>
        /// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual JArray FromRDF(RDFDataset dataset)
        {
// 1)
            JObject defaultGraph = new JObject();
// 2)
            JObject graphMap = new JObject();
            graphMap["@default"] = defaultGraph;
// 3/3.1)
            foreach (string name in dataset.GraphNames())
            {
                IList<RDFDataset.Quad> graph = dataset.GetQuads(name);
// 3.2+3.4)
                JObject nodeMap;
                if (!graphMap.ContainsKey(name))
                {
                    nodeMap = new JObject();
                    graphMap[name] = nodeMap;
                }
                else
                {
                    nodeMap = (JObject) graphMap[name];
                }

// 3.3)
                if (!"@default".Equals(name) && !Obj.Contains(defaultGraph, name))
                {
                    defaultGraph[name] = new JsonLdApi.NodeMapNode(this, name);
                }

// 3.5)
                foreach (RDFDataset.Quad triple in graph)
                {
                    string subject = triple.GetSubject().GetValue();
                    string predicate = triple.GetPredicate().GetValue();
                    RDFDataset.Node @object = triple.GetObject();
// 3.5.1+3.5.2)
                    JsonLdApi.NodeMapNode node;
                    if (!nodeMap.ContainsKey(subject))
                    {
                        node = new JsonLdApi.NodeMapNode(this, subject);
                        nodeMap[subject] = node;
                    }
                    else
                    {
                        node = (NodeMapNode) nodeMap[subject];
                    }

// 3.5.3)
                    if ((@object.IsIRI() || @object.IsBlankNode()) && !nodeMap.ContainsKey(@object.GetValue
                            ()))
                    {
                        nodeMap[@object.GetValue()] = new JsonLdApi.NodeMapNode(this, @object.GetValue());
                    }

// 3.5.4)
                    if (JsonldConsts.RdfType.Equals(predicate) && (@object.IsIRI() || @object.IsBlankNode
                                                                       ()) && !opts.UseRdfType)
                    {
                        JsonLdUtils.MergeValue(node, "@type", @object.GetValue());
                        continue;
                    }

// 3.5.5)
                    JObject value = @object.ToObject(opts.UseNativeTypes);
// 3.5.6+7)
                    JsonLdUtils.MergeValue(node, predicate, value);
// 3.5.8)
                    if (@object.IsBlankNode() || @object.IsIRI())
                    {
// 3.5.8.1-3)
                        ((NodeMapNode) nodeMap[@object.GetValue()]).usages.Add(new JsonLdApi.UsagesNode(this, node,
                            predicate
                            , value));
                    }
                }
            }

// 4)
            foreach (var name_1 in graphMap.GetKeys())
            {
                var graph = (JObject) graphMap[name_1];
// 4.1)
                if (!graph.ContainsKey(JsonldConsts.RdfNil))
                {
                    continue;
                }

// 4.2)
                var nil = (NodeMapNode) graph[JsonldConsts.RdfNil];
// 4.3)
                foreach (var usage in nil.usages)
                {
// 4.3.1)
                    var node = usage.node;
                    var property = usage.property;
                    var head = usage.value;
// 4.3.2)
                    var list = new JArray();
                    var listNodes = new JArray();
// 4.3.3)
                    while (JsonldConsts.RdfRest.Equals(property) && node.IsWellFormedListNode())
                    {
// 4.3.3.1)
                        list.Add(((JArray) node[JsonldConsts.RdfFirst])[0]);
// 4.3.3.2)
                        listNodes.Add((string) node["@id"]);
// 4.3.3.3)
                        var nodeUsage = node.usages[0];
// 4.3.3.4)
                        node = nodeUsage.node;
                        property = nodeUsage.property;
                        head = nodeUsage.value;
// 4.3.3.5)
                        if (!JsonLd.IsBlankNode(node))
                        {
                            break;
                        }
                    }

// 4.3.4)
                    if (JsonldConsts.RdfFirst.Equals(property))
                    {
// 4.3.4.1)
                        if (JsonldConsts.RdfNil.Equals(node["@id"]))
                        {
                            continue;
                        }

// 4.3.4.3)
                        string headId = (string) head["@id"];
// 4.3.4.4-5)
                        head = (JObject) ((JArray) graph[headId][JsonldConsts.RdfRest
                        ])[0];
// 4.3.4.6)
                        list.RemoveAt(list.Count - 1);
                        listNodes.RemoveAt(listNodes.Count - 1);
                    }

// 4.3.5)
                    JsonLD.Collections.Remove(head, "@id");
// 4.3.6)
                    JsonLD.Collections.Reverse(list);
// 4.3.7)
                    head["@list"] = list;
// 4.3.8)
                    foreach (string nodeId in listNodes)
                    {
                        JsonLD.Collections.Remove(graph, nodeId);
                    }
                }
            }

// 5)
            var result = new JArray();
// 6)
            var ids = new JArray(defaultGraph.GetKeys());
            ids.SortInPlace();
            foreach (string subject_1 in ids)
            {
                var node = (NodeMapNode) defaultGraph[subject_1];
// 6.1)
                if (graphMap.ContainsKey(subject_1))
                {
// 6.1.1)
                    node["@graph"] = new JArray();
// 6.1.2)
                    var keys = new JArray(graphMap[subject_1].GetKeys());
                    keys.SortInPlace();
                    foreach (string s in keys)
                    {
                        var n = (NodeMapNode) graphMap[subject_1][s];
                        if (n.Count == 1 && n.ContainsKey("@id"))
                        {
                            continue;
                        }

                        ((JArray) node["@graph"]).Add(n.Serialize());
                    }
                }

// 6.2)
                if (node.Count == 1 && node.ContainsKey("@id"))
                {
                    continue;
                }

                result.Add(node.Serialize());
            }

            return result;
        }

        /// <summary>Adds RDF triples for each graph in the given node map to an RDF dataset.
        /// 	</summary>
        /// <remarks>Adds RDF triples for each graph in the given node map to an RDF dataset.
        /// 	</remarks>
        /// <returns>the RDF dataset.</returns>
        /// <exception cref="JsonLdError">JsonLdError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual RDFDataset ToRDF()
        {
// TODO: make the default generateNodeMap call (i.e. without a
// graphName) create and return the nodeMap
            JObject nodeMap = new JObject();
            nodeMap["@default"] = new JObject();
            GenerateNodeMap(this.value, nodeMap);
            RDFDataset dataset = new RDFDataset(this);
            foreach (string graphName in nodeMap.GetKeys())
            {
// 4.1)
                if (graphName.IsRelativeIri())
                {
                    continue;
                }

                JObject graph = (JObject) nodeMap[graphName
                ];
                dataset.GraphToRDF(graphName, graph);
            }

            return dataset;
        }

        /// <summary>Performs RDF normalization on the given JSON-LD input.</summary>
        /// <remarks>Performs RDF normalization on the given JSON-LD input.</remarks>
        /// <param name="input">the expanded JSON-LD object to normalize.</param>
        /// <param name="options">the normalization options.</param>
        /// <param name="callback">(err, normalized) called once the operation completes.</param>
        /// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual object Normalize(RDFDataset dataset)
        {
// create quads and map bnodes to their associated quads
            IList<RDFDataset.Quad> quads = new List<RDFDataset.Quad>();
            IDictionary<string, IDictionary<string, object>> bnodes =
                new Dictionary<string, IDictionary<string, object>>();
            foreach (string graphName in dataset.Keys)
            {
                var eachGraphName = graphName;
                IList<RDFDataset.Quad> triples = (IList<RDFDataset.Quad>) dataset[eachGraphName];
                if ("@default".Equals(eachGraphName))
                {
                    eachGraphName = null;
                }

                foreach (RDFDataset.Quad quad in triples)
                {
                    if (eachGraphName != null)
                    {
                        if (eachGraphName.IndexOf("_:") == 0)
                        {
                            IDictionary<string, object> tmp = new Dictionary<string, object>();
                            tmp["type"] = "blank node";
                            tmp["value"] = eachGraphName;
                            quad["name"] = tmp;
                        }
                        else
                        {
                            IDictionary<string, object> tmp = new Dictionary<string, object>();
                            tmp["type"] = "IRI";
                            tmp["value"] = eachGraphName;
                            quad["name"] = tmp;
                        }
                    }

                    quads.Add(quad);
                    string[] attrs = new string[] {"subject", "object", "name"};
                    foreach (string attr in attrs)
                    {
                        if (quad.ContainsKey(attr) &&
                            (string) ((IDictionary<string, object>) quad[attr])["type"] == "blank node")
                        {
                            string id = (string) ((IDictionary<string, object>) quad[attr])["value"];
                            if (!bnodes.ContainsKey(id))
                            {
                                bnodes[id] = new Dictionary<string, object> {{"quads", new List<RDFDataset.Quad>()}};
                            }

                            ((IList<RDFDataset.Quad>) bnodes[id]["quads"]).Add(quad);
                        }
                    }
                }
            }

// mapping complete, start canonical naming
            NormalizeUtils normalizeUtils = new NormalizeUtils(quads, bnodes, new UniqueNamer
                ("_:c14n"), opts);
            return normalizeUtils.HashBlankNodes(bnodes.Keys);
        }
    }
}