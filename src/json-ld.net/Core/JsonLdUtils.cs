using System;
using System.Collections.Generic;
using JsonLD.Core;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    public class JsonLdUtils
    {
        private const int MaxContextUrls = 10;

        private static bool DeepCompare(JToken v1, JToken v2, bool listOrderMatters)
        {
            if (v1 == null)
            {
                return v2 == null;
            }

            if (v2 == null)
            {
                return false;
            }

            if (v1 is JObject m1 && v2 is JObject m2)
            {
                if (m1.Count != m2.Count)
                {
                    return false;
                }
                foreach (string key in m1.GetKeys())
                {
                    if (!((IDictionary<string,JToken>)m2).ContainsKey(key) ||
                        !DeepCompare(m1[key], m2[key], listOrderMatters))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (v1 is JArray && v2 is JArray)
            {
                JArray l1 = (JArray)v1;
                JArray l2 = (JArray)v2;
                var l1Count = l1.Count;
                var l2Count = l2.Count;
                if (l1Count != l2Count)
                {
                    return false;
                }
                // used to mark members of l2 that we have already matched to avoid
                // matching the same item twice for lists that have duplicates
                bool[] alreadyMatched = new bool[l2Count];
                for (int i = 0; i < l1Count; i++)
                {
                    JToken o1 = l1[i];
                    bool gotmatch = false;
                    if (listOrderMatters)
                    {
                        gotmatch = DeepCompare(o1, l2[i], listOrderMatters);
                    }
                    else
                    {
                        for (int j = 0; j < l2Count; j++)
                        {
                            if (!alreadyMatched[j] && DeepCompare(o1, l2[j], listOrderMatters))
                            {
                                alreadyMatched[j] = true;
                                gotmatch = true;
                                break;
                            }
                        }
                    }
                    if (!gotmatch)
                    {
                        return false;
                    }
                }
                return true;
            }

            
            var v1String = v1.ToString().Replace("\r\n", "").Replace("\n", "").Replace("http:", "https:");
            var v2String = v2.ToString().Replace("\r\n", "").Replace("\n", "").Replace("http:", "https:");
            return v1String.Equals(v2String);
            
            
        }

        public static bool DeepCompare(JToken v1, JToken v2)
        {
            return DeepCompare(v1, v2, false);
        }

        public static bool DeepContains(JArray values, JToken value)
        {
            foreach (JToken item in values)
            {
                if (DeepCompare(item, value, false))
                {
                    return true;
                }
            }
            return false;
        }

        internal static void MergeValue(JObject obj, string key, JToken value, bool skipSetContainsCheck = false)
        {
            if (obj == null)
            {
                return;
            }
            var values = (JArray)obj[key];
            if (values == null)
            {
                values = new JArray();
                obj[key] = values;
            }
            if (skipSetContainsCheck ||
                "@list".Equals(key) ||
                (value is JObject && ((IDictionary<string, JToken>)value).ContainsKey("@list")) ||
                !DeepContains(values, (JToken)value))
            {
                values.Add(value);
            }
        }


        /// <summary>Returns true if the given value is a subject with properties.</summary>
        /// <remarks>Returns true if the given value is a subject with properties.</remarks>
        /// <param name="v">the value to check.</param>
        /// <returns>true if the value is a subject with properties, false if not.</returns>
        internal static bool IsNode(JToken v)
        {
            // Note: A value is a subject if all of these hold true:
            // 1. It is an Object.
            // 2. It is not a @value, @set, or @list.
            // 3. It has more than 1 key OR any existing key is not @id.
            if (v is JObject && !(((IDictionary<string, JToken>)v).ContainsKey("@value") || ((IDictionary<string, JToken>
                )v).ContainsKey("@set") || ((IDictionary<string, JToken>)v).ContainsKey("@list")))
            {
                return ((IDictionary<string, JToken>)v).Count > 1 || !((IDictionary<string, JToken>)v).ContainsKey
                    ("@id");
            }
            return false;
        }

        /// <summary>Returns true if the given value is a subject reference.</summary>
        /// <remarks>Returns true if the given value is a subject reference.</remarks>
        /// <param name="v">the value to check.</param>
        /// <returns>true if the value is a subject reference, false if not.</returns>
        internal static bool IsNodeReference(JToken v)
        {
            // Note: A value is a subject reference if all of these hold true:
            // 1. It is an Object.
            // 2. It has a single key: @id.
            return (v is JObject && ((IDictionary<string, JToken>)v).Count == 1 && ((IDictionary
                <string, JToken>)v).ContainsKey("@id"));
        }

        // //////////////////////////////////////////////////// OLD CODE BELOW

        /// <summary>Compares two strings first based on length and then lexicographically.</summary>
        /// <remarks>Compares two strings first based on length and then lexicographically.</remarks>
        /// <param name="a">the first string.</param>
        /// <param name="b">the second string.</param>
        /// <returns>-1 if a &lt; b, 1 if a &gt; b, 0 if a == b.</returns>
        internal static int CompareShortestLeast(string a, string b)
        {
            if (a.Length < b.Length)
            {
                return -1;
            }

            if (b.Length < a.Length)
            {
                return 1;
            }
            return Math.Sign(string.CompareOrdinal(a, b));
        }

        /// <summary>Compares two JSON-LD values for equality.</summary>
        /// <remarks>
        /// Compares two JSON-LD values for equality. Two JSON-LD values will be
        /// considered equal if:
        /// 1. They are both primitives of the same type and value. 2. They are both @values
        /// with the same @value, @type, and @language, OR 3. They both have @ids
        /// they are the same.
        /// </remarks>
        /// <param name="v1">the first value.</param>
        /// <param name="v2">the second value.</param>
        /// <returns>true if v1 and v2 are considered equal, false if not.</returns>
        internal static bool CompareValues(JToken v1, JToken v2)
        {
            if (v1.Equals(v2))
            {
                return true;
            }
            if (v1.IsValue() && v2.IsValue() && Obj.Equals(((JObject)v1)["@value"
                ], ((JObject)v2)["@value"]) && Obj.Equals(((JObject)v1)["@type"], ((JObject)v2)["@type"]) && Obj.Equals
                (((JObject)v1)["@language"], ((JObject)v2
                )["@language"]) && Obj.Equals(((JObject)v1)["@index"], ((JObject)v2)["@index"]))
            {
                return true;
            }
            return (v1 is JObject o && o.ContainsKey("@id")) &&
                   (v2 is JObject jObject && jObject.ContainsKey("@id")) &&
                   o["@id"].Equals(jObject["@id"]);
        }
    }
}
