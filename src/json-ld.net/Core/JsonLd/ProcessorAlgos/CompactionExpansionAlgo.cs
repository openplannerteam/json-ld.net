using System;
using System.Collections.Generic;
using JsonLD.Core.ContextAlgos;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core.ProcessorAlgos
{
    /// <summary>
    /// Compaction and expansion algorithm of a context and the entire JSON-LD
    /// </summary>
    internal class CompactionExpansionAlgo
    {
        // I sincerely apologize to anyone reading this class that he/she should deal with a class of over 1000 LoC.
        // But then, one of the methods was > 700LoC when I started refactoring

        private readonly IDocumentLoader _loader;
        private readonly JsonLdOptions _opts;

        public CompactionExpansionAlgo(IDocumentLoader loader, JsonLdOptions opts)
        {
            _loader = loader;
            _opts = opts;
        }

        // Part of the compaction algorithm
        private void HandleExpandedElement(Context activeCtx, JObject result, string expandedProperty,
            bool compactArrays, bool insideReverse, JToken expandedItem)
        {
            // 7.6.1)
            var itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedItem, true, insideReverse);
            // 7.6.2)
            var container = activeCtx.GetContainer(itemActiveProperty);
            // get @list value if appropriate
            var isList = expandedItem.IsDictContaining("@list");

            // 7.6.3)
            var compactedItem = Compact(activeCtx, itemActiveProperty,
                isList ? ((IDictionary<string, JToken>) expandedItem)["@list"] : expandedItem
                , compactArrays);

            // 7.6.4)
            if (isList)
            {
                // 7.6.4.1)
                if (!(compactedItem is JArray))
                {
                    compactedItem = new JArray {compactedItem};
                }

                // 7.6.4.2)
                if (!"@list".Equals(container))
                {
                    // 7.6.4.2.1)
                    // TODO: SPEC: no mention of vocab = true
                    var wrapper = new JObject
                    {
                        [activeCtx.CompactIri("@list", true)] = compactedItem
                    };
                    compactedItem = wrapper;
                    // 7.6.4.2.2)
                    if (expandedItem.IsDictContaining("@index", out var dict))
                    {
                        dict[activeCtx.CompactIri("@index", true)] = dict["@index"];
                    }
                }
                else if (result.ContainsKey(itemActiveProperty))
                {
                    // TODO: SPEC: no mention of vocab =
                    // 7.6.4.3)
                    throw new JsonLdError(JsonLdError.Error.CompactionToListOfLists,
                        "There cannot be two list objects associated with an active property that has a container mapping"
                    );
                }
            }

            // 7.6.5)
            if ("@language".Equals(container)
                || "@index".Equals(container))
            {
                // 7.6.5.1)
                var mapObject = new JObject();
                if (result.ContainsKey(itemActiveProperty))
                {
                    mapObject = (JObject) result[itemActiveProperty];
                }
                else
                {
                    result[itemActiveProperty] = mapObject;
                }

                // 7.6.5.2)
                if ("@language".Equals(container)
                    && compactedItem.IsDictContaining("@value"))
                {
                    compactedItem = compactedItem["@value"];
                }

                // 7.6.5.3)
                var mapKey = (string) expandedItem[container];
                // 7.6.5.4)
                if (!mapObject.ContainsKey(mapKey))
                {
                    mapObject[mapKey] = compactedItem;
                }
                else
                {
                    if (!(mapObject[mapKey] is JArray))
                    {
                        mapObject[mapKey] = new JArray {mapObject[mapKey]};
                    }

                    ((JArray) mapObject[mapKey]).Add(compactedItem);
                }
            }
            else
            {
                // 7.6.6)
                // 7.6.6.1)
                if ((!compactArrays
                     || "@set".Equals(container)
                     || "@list".Equals(container)
                     || "@list".Equals(expandedProperty)
                     || "@graph".Equals(expandedProperty))
                    && !compactedItem.IsArray())
                {
                    compactedItem = new JArray {compactedItem};
                }

                // 7.6.6.2)
                if (!result.ContainsKey(itemActiveProperty))
                {
                    result[itemActiveProperty] = compactedItem;
                }
                else
                {
                    if (!result[itemActiveProperty].IsArray())
                    {
                        result[itemActiveProperty] = new JArray {result[itemActiveProperty]};
                    }

                    if (compactedItem.IsArray())
                    {
                        Collections.AddAll((JArray) result[itemActiveProperty],
                            (JArray) compactedItem);
                    }
                    else
                    {
                        ((JArray) result[itemActiveProperty]).Add(compactedItem);
                    }
                }
            }
        }

        // Part of the compaction algorithm
        private void CompactElement(Context activeCtx, JObject result, JObject elem, string expandedProperty,
            bool compactArrays, bool insideReverse, string activeProperty)
        {
            var expandedValue = elem[expandedProperty];
            // 7.1)
            if ("@id".Equals(expandedProperty) || "@type".Equals(expandedProperty))
            {
                JToken compactedValue;
                // 7.1.1)
                if (expandedValue.IsString())
                {
                    compactedValue = activeCtx.CompactIri((string) expandedValue,
                        "@type".Equals(expandedProperty));
                }
                else
                {
                    // 7.1.2)
                    var types = new JArray();
                    // 7.1.2.2)
                    foreach (string expandedType in (JArray) expandedValue)
                    {
                        types.Add(activeCtx.CompactIri(expandedType, true));
                    }

                    // 7.1.2.3)
                    compactedValue = types.Count == 1 ? types[0] : types;
                }

                // 7.1.3)
                var alias = activeCtx.CompactIri(expandedProperty, true);
                // 7.1.4)
                result[alias] = compactedValue;
                return;
            }

            // 7.2)
            if ("@reverse".Equals(expandedProperty))
            {
                // 7.2.1)
                var compactedValue = (JObject) Compact(activeCtx, "@reverse", expandedValue, compactArrays);
                // 7.2.2)
                var properties = new List<string>(compactedValue.GetKeys());
                foreach (var property in properties)
                {
                    var value = compactedValue[property];
                    // 7.2.2.1)
                    if (!activeCtx.IsReverseProperty(property)) continue;


                    // 7.2.2.1.1)
                    if (("@set".Equals(activeCtx.GetContainer(property)) || !compactArrays) && !(value
                            is JArray))
                    {
                        result[property] = new JArray {value};
                    }

                    // 7.2.2.1.2)
                    if (!result.ContainsKey(property))
                    {
                        result[property] = value;
                    }
                    else
                    {
                        // 7.2.2.1.3)
                        if (!(result[property] is JArray))
                        {
                            var tmp = new JArray {result[property]};
                            result[property] = tmp;
                        }

                        if (value is JArray array)
                        {
                            Collections.AddAll(((JArray) result[property]), array);
                        }
                        else
                        {
                            ((JArray) result[property]).Add(value);
                        }
                    }

                    // 7.2.2.1.4) TODO: this doesn't seem safe (i.e.
                    // modifying the map being used to drive the loop)!
                    Collections.Remove(compactedValue, property);
                }

                // 7.2.3)
                if (compactedValue.Count == 0) return;

                // 7.2.3.1)
                // 7.2.3.2)
                result[activeCtx.CompactIri("@reverse", true)] = compactedValue;
                // 7.2.4)
                return;
            }

            // 7.3)
            if ("@index".Equals(expandedProperty) && "@index".Equals(activeCtx.GetContainer(activeProperty)))
            {
                return;
            }

            // 7.4)
            if ("@index".Equals(expandedProperty) || "@value".Equals(expandedProperty) || "@language"
                    .Equals(expandedProperty))
            {
                // 7.4.1)
                // 7.4.2)
                result[activeCtx.CompactIri(expandedProperty, true)] = expandedValue;
                return;
            }

            // NOTE: expanded value must be an array due to expansion
            // algorithm.
            // 7.5)
            if (((JArray) expandedValue).Count == 0)
            {
                // 7.5.1)
                var itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedValue,
                    true, insideReverse);
                // 7.5.2)
                if (!result.ContainsKey(itemActiveProperty))
                {
                    result[itemActiveProperty] = new JArray();
                }
                else if (!(result[itemActiveProperty] is JArray))
                {
                    result[itemActiveProperty] = new JArray {result[itemActiveProperty]};
                }
            }


            // 7.6)
            foreach (var expandedItem in (JArray) expandedValue)
            {
                HandleExpandedElement(activeCtx, result, expandedProperty, compactArrays, insideReverse,
                    expandedItem);
            }
        }

        /// <summary>
        /// Compaction Algorithm
        /// http://json-ld.org/spec/latest/json-ld-api/#compaction-algorithm
        /// </summary>
        /// <param name="activeCtx"></param>
        /// <param name="activeProperty"></param>
        /// <param name="element"></param>
        /// <param name="compactArrays"></param>
        /// <returns></returns>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public JToken Compact(Context activeCtx, string activeProperty, JToken element
            , bool compactArrays)
        {
            // 2)
            if (element is JArray)
            {
                // 2.1)
                var resultArr = new JArray();
                // 2.2)
                foreach (var item in element)
                {
                    // 2.2.1)
                    var compactedItem = Compact(activeCtx, activeProperty, item, compactArrays);
                    // 2.2.2)
                    if (!compactedItem.IsNull())
                    {
                        resultArr.Add(compactedItem);
                    }
                }

                // 2.3)
                if (compactArrays && resultArr.Count == 1 && activeCtx.GetContainer(activeProperty) == null)
                {
                    return resultArr[0];
                }

                // 2.4)
                return resultArr;
            }


            // 3)
            if (!(element is JObject elem)) return element;


            // 4
            if (elem.ContainsKey("@value") || elem.ContainsKey("@id"))
            {
                var compactedValue = activeCtx.CompactValue(activeProperty, elem);
                if (!(compactedValue is JObject || compactedValue is JArray))
                {
                    return compactedValue;
                }
            }

            // 5)
            var insideReverse = ("@reverse".Equals(activeProperty));
            // 6)
            var result = new JObject();
            // 7)
            var keys = new JArray(elem.GetKeys());
            keys.SortInPlace();

            foreach (string expandedProperty in keys)
            {
                // TODO
                CompactElement(activeCtx, result, elem, expandedProperty, compactArrays, insideReverse, activeProperty);
            }

            // 8)
            return result;
        }


        /// <summary>
        ///  Compaction of a context
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public JObject CompactContext(JToken input, JToken context)
        {
            // 1)
            // 2-6) NOTE: these are all the same steps as in expand
            JToken expanded = ExpandContext(input, _opts);
            // 7)
            if (context.IsDictContaining("@context", out var dct))
            {
                context = dct["@context"];
            }


            var activeCtx = new Context(_opts);
            var parsingAlgo = new ParsingAlgorithm(activeCtx, _loader);
            activeCtx = parsingAlgo.ParseContext(context);

            // 8)
            var compacted = Compact(activeCtx, null, expanded, _opts.CompactArrays);
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

        // Inner loop of the expand-JSON-LD
        private void ExpandElement(Context activeCtx, string activeProperty, JObject elem, string key, JObject result)
        {
            JToken value = elem[key];
            // 7.1)
            if (key.Equals("@context"))
            {
                return;
            }

            // 7.2)
            string expandedProperty = activeCtx.ExpandIri(key, false, true, null, null);
            JToken expandedValue = null;
            // 7.3)
            if (expandedProperty == null || (!expandedProperty.Contains(":") && !JsonLd.IsKeyword
                                                 (expandedProperty)))
            {
                return;
            }

            // 7.4)
            if (JsonLd.IsKeyword(expandedProperty))
            {
                // 7.4.1)
                if ("@reverse".Equals(activeProperty))
                {
                    throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyMap,
                        "a keyword cannot be used as a @reverse property");
                }

                // 7.4.2)
                if (result.ContainsKey(expandedProperty))
                {
                    throw new JsonLdError(JsonLdError.Error.CollidingKeywords,
                        expandedProperty + " already exists in result");
                }

                switch (expandedProperty)
                {
                    // 7.4.3)
                    case "@id" when !value.IsString():
                        throw new JsonLdError(JsonLdError.Error.InvalidIdValue,
                            "value of @id must be a string");
                    case "@id":
                        expandedValue = activeCtx.ExpandIri((string) value, true, false, null, null);
                        break;
                    // 7.4.4)
                    case "@type" when value is JArray array:
                    {
                        expandedValue = new JArray();
                        foreach (var v in array)
                        {
                            if (v.Type != JTokenType.String)
                            {
                                throw new JsonLdError(JsonLdError.Error.InvalidTypeValue,
                                    "@type value must be a string or array of strings");
                            }

                            ((JArray) expandedValue).Add(
                                activeCtx.ExpandIri((string) v, true, true, null, null));
                        }

                        break;
                    }
                    case "@type" when value.IsString():
                        expandedValue = activeCtx.ExpandIri((string) value, true, true, null, null);
                        break;
                    case "@type":
                    {
                        // TODO: SPEC: no mention of empty map check
                        if (!(value is JObject o))
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidTypeValue,
                                "@type value must be a string or array of strings");
                        }

                        if (o.Count != 0)
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidTypeValue,
                                "@type value must be a an empty object for framing");
                        }

                        expandedValue = o;
                        break;
                    }
                    // 7.4.5)
                    case "@graph":
                        expandedValue = Expand(activeCtx, "@graph", value);
                        break;
                    // 7.4.6)
                    case "@value" when !value.IsNull() && (value is JObject || value is JArray):
                        throw new JsonLdError(JsonLdError.Error.InvalidValueObjectValue,
                            $"value of {expandedProperty} must be a scalar or null");
                    case "@value":
                    {
                        expandedValue = value;
                        if (expandedValue.IsNull())
                        {
                            result["@value"] = null;
                            return;
                        }

                        break;
                    }
                    // 7.4.7)
                    case "@language" when !value.IsString():
                        throw new JsonLdError(JsonLdError.Error.InvalidLanguageTaggedString,
                            $"Value of  {expandedProperty} must be a string");
                    case "@language":
                        expandedValue = ((string) value).ToLower();
                        break;
                    // 7.4.8)
                    case "@index" when !value.IsString():
                        throw new JsonLdError(JsonLdError.Error.InvalidIndexValue,
                            $"Value of {expandedProperty} must be a string");
                    case "@index":
                        expandedValue = value;
                        break;
                    // 7.4.9)
                    // 7.4.9.1)
                    case "@list" when activeProperty == null || "@graph".Equals(activeProperty):
                        return;
                    // 7.4.9.2)
                    case "@list":
                    {
                        expandedValue = Expand(activeCtx, activeProperty, value);
                        // NOTE: step not in the spec yet
                        if (!(expandedValue is JArray))
                        {
                            expandedValue = new JArray {expandedValue};
                        }

                        // 7.4.9.3)
                        foreach (var o in (JArray) expandedValue)
                        {
                            if (o.IsDictContaining("@list"))
                            {
                                throw new JsonLdError(JsonLdError.Error.ListOfLists,
                                    "A list may not contain another list");
                            }
                        }

                        break;
                    }
                    // 7.4.10)
                    case "@set":
                        expandedValue = Expand(activeCtx, activeProperty, value);
                        break;

                    // 7.4.11)
                    case "@reverse" when !(value is JObject):
                        throw new JsonLdError(
                            JsonLdError.Error.InvalidReverseValue,
                            "@reverse value must be an object"
                        );
                    // 7.4.11.1)
                    case "@reverse":
                    {
                        var expandedValueD = (JObject) Expand(activeCtx, "@reverse", value);
                        // NOTE: algorithm assumes the result is a map
                        // 7.4.11.2)
                        if (expandedValueD
                            .IsDictContaining("@reverse", out var dict))
                        {
                            var reverse = (JObject) dict["@reverse"];
                            foreach (var property in reverse.GetKeys())
                            {
                                var item = reverse[property];
                                // 7.4.11.2.1)
                                if (!result.ContainsKey(property))
                                {
                                    result[property] = new JArray();
                                }

                                // 7.4.11.2.2)
                                if (item is JArray array)
                                {
                                    Collections.AddAll(
                                        ((JArray) result[property]),
                                        array);
                                }
                                else
                                {
                                    ((JArray) result[property]).Add(item);
                                }
                            }
                        }

                        // 7.4.11.3)
                        if (expandedValueD.Count == 0
                            || (expandedValueD.Count == 1
                                && expandedValueD.ContainsKey("@reverse")))
                        {
                            return;
                        }


                        // 7.4.11.3.1)
                        if (!result.ContainsKey("@reverse"))
                        {
                            result["@reverse"] = new JObject();
                        }

                        // 7.4.11.3.2)
                        var reverseMap = (JObject) result["@reverse"];
                        // 7.4.11.3.3)
                        foreach (var property in expandedValueD.GetKeys())
                        {
                            if ("@reverse".Equals(property))
                            {
                                continue;
                            }

                            // 7.4.11.3.3.1)
                            var items = (JArray) expandedValueD[property];
                            foreach (var item in items)
                            {
                                // 7.4.11.3.3.1.1)
                                if (item.IsDictContaining("@value")
                                    || item.IsDictContaining("@list"))
                                {
                                    throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
                                }

                                // 7.4.11.3.3.1.2)
                                if (!reverseMap.ContainsKey(property))
                                {
                                    reverseMap[property] = new JArray();
                                }

                                // 7.4.11.3.3.1.3)
                                ((JArray) reverseMap[property]).Add(item);
                            }
                        }

                        // 7.4.11.4)
                        return;
                    }
                    // TODO: SPEC no mention of @explicit etc in spec
                    case "@explicit":
                    case "@default":
                    case "@embed":
                    case "@embedChildren":
                    case "@omitDefault":
                        expandedValue = Expand(activeCtx, expandedProperty,
                            value);
                        break;
                }

                // 7.4.12)
                if (!expandedValue.IsNull())
                {
                    result[expandedProperty] = expandedValue;
                }

                // 7.4.13)
                return;
            }


            /////////////////////////// 7.5 ///////////////////////////////////////:


            // 7.5
            if ("@language".Equals(activeCtx.GetContainer(key)) && value is JObject jObject)
            {
                // 7.5.1)
                expandedValue = new JArray();
                // 7.5.2)
                foreach (var language in jObject.GetKeys())
                {
                    var languageValue = jObject[language];
                    // 7.5.2.1)
                    if (!(languageValue is JArray))
                    {
                        var tmp = languageValue;
                        languageValue = new JArray() {tmp};
                    }

                    // 7.5.2.2)
                    foreach (var item in (JArray) languageValue)
                    {
                        // 7.5.2.2.1)
                        if (!item.IsString())
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidLanguageMapValue,
                                "Expected " + item + " to be a string");
                        }

                        // 7.5.2.2.2)
                        ((JArray) expandedValue).Add(
                            new JObject
                            {
                                ["@value"] = item,
                                ["@language"] = language.ToLower()
                            });
                    }
                }
            }
            else if ("@index".Equals(activeCtx.GetContainer(key)) && value is JObject o)
            {
                // 7.6)
                // 7.6.1)
                expandedValue = new JArray();
                // 7.6.2)
                var indexKeys = new JArray(o.GetKeys());
                indexKeys.SortInPlace();
                foreach (string index in indexKeys)
                {
                    var indexValue = o[index];
                    // 7.6.2.1)
                    if (!(indexValue is JArray))
                    {
                        indexValue = new JArray() {indexValue};
                    }

                    // 7.6.2.2)
                    indexValue = Expand(activeCtx, key, indexValue);
                    // 7.6.2.3)
                    foreach (var jToken in (JArray) indexValue)
                    {
                        var item = (JObject) jToken;
                        // 7.6.2.3.1)
                        if (!item.ContainsKey("@index"))
                        {
                            item["@index"] = index;
                        }

                        // 7.6.2.3.2)
                        ((JArray) expandedValue).Add(item);
                    }
                }
            }
            else
            {
                // 7.7)
                expandedValue = Expand(activeCtx, key, value);
            }

            // 7.8)
            if (expandedValue.IsNull())
            {
                return;
            }

            // 7.9)
            if ("@list".Equals(activeCtx.GetContainer(key)))
            {
                if (!expandedValue.IsDictContaining("@list"))
                {
                    var tmp = expandedValue;
                    if (!(tmp is JArray))
                    {
                        tmp = new JArray();
                        ((JArray) tmp).Add(expandedValue);
                    }

                    expandedValue = new JObject();
                    ((JObject) expandedValue)["@list"] = tmp;
                }
            }


            // 7.10)
            if (activeCtx.IsReverseProperty(key))
            {
                // 7.10.1)
                if (!result.ContainsKey("@reverse"))
                {
                    result["@reverse"] = new JObject();
                }

                // 7.10.2)
                var reverseMap = (JObject) result["@reverse"];
                // 7.10.3)
                if (!expandedValue.IsArray())
                {
                    expandedValue = new JArray() {expandedValue};
                }

                // 7.10.4)
                foreach (var item in (JArray) expandedValue)
                {
                    // 7.10.4.1)
                    if (item.IsDictContaining("@value") || item.IsDictContaining("@list"))
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
                    }

                    // 7.10.4.2)
                    if (!reverseMap.ContainsKey(expandedProperty))
                    {
                        reverseMap[expandedProperty] = new JArray();
                    }

                    // 7.10.4.3)
                    if (item is JArray array)
                    {
                        Collections.AddAll(((JArray) reverseMap[expandedProperty]), array);
                    }
                    else
                    {
                        ((JArray) reverseMap[expandedProperty]).Add(item);
                    }
                }
            }
            else
            {
                // 7.11)
                // 7.11.1)
                if (!result.ContainsKey(expandedProperty))
                {
                    result[expandedProperty] = new JArray();
                }

                // 7.11.2)
                if (expandedValue is JArray array)
                {
                    Collections.AddAll(((JArray) result[expandedProperty]), array);
                }
                else
                {
                    ((JArray) result[expandedProperty]).Add(expandedValue);
                }
            }
        }


        /// <summary>
        /// Expansion Algorithm
        /// http://json-ld.org/spec/latest/json-ld-api/#expansion-algorithm
        /// </summary>
        /// <param name="activeCtx"></param>
        /// <param name="activeProperty"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        /// <exception cref="JsonLdError">JsonLdError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public JToken Expand(Context activeCtx, string activeProperty, JToken element)
        {
            // 1)
            if (element.IsNull())
            {
                return null;
            }

            // 3)
            if (element is JArray array)
            {
                // 3.1)
                var result = new JArray();
                // 3.2)
                foreach (var item in array)
                {
                    // 3.2.1)
                    var v = Expand(activeCtx, activeProperty, item);
                    // 3.2.2)
                    if (("@list".Equals(activeProperty)
                         || "@list".Equals(activeCtx.GetContainer(activeProperty)))
                        && (v is JArray
                            || v.IsDictContaining("@list")))
                    {
                        throw new JsonLdError(JsonLdError.Error.ListOfLists, "lists of lists are not permitted.");
                    }

                    // 3.2.3)
                    if (v.IsNull())
                    {
                        continue;
                    }


                    if (v is JArray jArray)
                    {
                        Collections.AddAll(result, jArray);
                    }
                    else
                    {
                        result.Add(v);
                    }
                }

                // 3.3)
                return result;
            }

            // 4)
            if (element is JObject jObject)
            {
                // 5)
                if (jObject.ContainsKey("@context"))
                {
                    var parser = new ParsingAlgorithm(activeCtx, _loader);
                    activeCtx = parser.ParseContext(jObject["@context"]);
                }

                // 6)
                var result = new JObject();
                // 7)
                var keys = new JArray(jObject.GetKeys());
                keys.SortInPlace();
                foreach (string key in keys)
                {
                    ExpandElement(activeCtx, activeProperty, jObject, key, result);
                }

                // 8)
                if (result.ContainsKey("@value"))
                {
                    // 8.1)
                    // TODO: is this method faster than just using containsKey for each?
                    ICollection<string> keySet = new HashSet<string>(result.GetKeys());
                    keySet.Remove("@value");
                    keySet.Remove("@index");
                    var langremoved = keySet.Remove("@language");
                    var typeremoved = keySet.Remove("@type");
                    if ((langremoved && typeremoved) || !keySet.IsEmpty())
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidValueObject, "value object has unknown keys");
                    }

                    // 8.2)
                    var rval = result["@value"];
                    if (rval.IsNull())
                    {
                        // nothing else is possible with result if we set it to
                        // null, so simply return it
                        return null;
                    }

                    // 8.3)
                    if (!rval.IsString() && result.ContainsKey("@language"))
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidLanguageTaggedValue,
                            "when @language is used, @value must be a string");
                    }

                    // 8.4)
                    if (result.ContainsKey("@type"))
                    {
                        // TODO: is this enough for "is an IRI"
                        if (!result["@type"].IsString() ||
                            ((string) result["@type"]).StartsWith("_:") ||
                            !((string) result["@type"]).Contains(":"))
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidTypedValue,
                                "value of @type must be an IRI"
                            );
                        }
                    }
                }
                else if (result.ContainsKey("@type"))
                {
                    // 9)
                    var rtype = result["@type"];
                    if (!(rtype is JArray))
                    {
                        var tmp = new JArray {rtype};
                        result["@type"] = tmp;
                    }
                }
                else if (result.ContainsKey("@set") || result.ContainsKey("@list"))
                {
                    // 10)
                    // 10.1)
                    if (result.Count > (result.ContainsKey("@index") ? 2 : 1))
                    {
                        throw new JsonLdError(JsonLdError.Error.InvalidSetOrListObject,
                            "@set or @list may only contain @index");
                    }

                    // 10.2)
                    if (result.ContainsKey("@set"))
                    {
                        // result becomes an array here, thus the remaining checks
                        // will never be true from here on
                        // so simply return the value rather than have to make
                        // result an object and cast it with every
                        // other use in the function.
                        return result["@set"];
                    }
                }

                // 11)
                if (result.ContainsKey("@language") && result.Count == 1)
                {
                    result = null;
                }

                // 12)
                if (activeProperty != null && !"@graph".Equals(activeProperty)) return result;


                // 12.1)
                if (result != null && (result.Count == 0 || result.ContainsKey("@value") || result
                                           .ContainsKey("@list")))
                {
                    result = null;
                }
                else if (result != null && result.ContainsKey("@id") && result.Count == 1)
                {
                    // 12.2)
                    result = null;
                }

                // 13)
                return result;
            }

            // 2) If element is a scalar
            // 2.1)
            if (activeProperty == null || "@graph".Equals(activeProperty))
            {
                return null;
            }

            return activeCtx.ExpandValue(activeProperty, element);
        }


        public JArray ExpandContext(JToken input, JsonLdOptions opts)
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
                input = opts.documentLoader.LoadDocument(new Uri((string) input));

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

                var parser = new ParsingAlgorithm(activeCtx, _loader);
                activeCtx = parser.ParseContext(exCtx);
            }

            // 5)
            // TODO: add support for getting a context from HTTP when content-type is set to a jsonld compatable format
            // 6)
            var expanded = Expand(activeCtx, null, input);
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