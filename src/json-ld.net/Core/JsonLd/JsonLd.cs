using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace JsonLD.Core
{
    /// <summary>
    /// This represents the 'JSON-LD' data structure.
    /// However, _there is no JSON-LD dat structure_.
    ///
    /// This library leverages the standard library's 'JObject' and uses that as main data structure.
    /// However, all JSON-LD specific functions are implemented here as extension methods, giving a Json-Ld-object impression
    /// </summary>
    public static class JsonLd
    {
        public static readonly IList<string> Keywords = new[]
        {
            "@base",
            "@context",
            "@container",
            "@default",
            "@embed",
            "@explicit",
            "@graph",
            "@id",
            "@index",
            "@language",
            "@list",
            "@omitDefault",
            "@reverse",
            "@preserve",
            "@set",
            "@type",
            "@value",
            "@vocab"
        };

        /// <summary>Returns whether or not the given value is a keyword (or a keyword alias).
        /// 	</summary>
        /// <returns>True if the value is a keyword, false if not.</returns>
        public static bool IsKeyword(JToken key)
        {
            if (!key.IsString())
            {
                return false;
            }

            var keyString = (string) key;
            return Keywords.Contains(keyString);
        }


        public static bool IsAbsoluteIri(this string value)
        {
            // TODO: this is a bit simplistic!
            return value != null && value.Contains(":");
        }

        public static bool IsRelativeIri(this string value)
        {
            return !(IsKeyword(value) || IsAbsoluteIri(value));
        }

        /// <summary>Returns true if the given value is a JSON-LD dictionary containing the key</summary>
        /// <param name="v">the value to check.</param>
        /// <param name="key">They key that should be contained</param>
        /// <returns></returns>
        internal static bool IsDictContaining(this JToken v, string key)
        {
            return IsDictContaining(v, key, out _);
        }

        /// <summary>Returns true if the given value is a JSON-LD dictionary containing the key</summary>
        /// <param name="v">the value to check.</param>
        /// <param name="key">They key that should be contained</param>
        /// <param name="dict">The type casted dictionary will be saved in this variable</param>
        /// <returns></returns>
        public static bool IsDictContaining(this JToken v, string key, out JObject dict)
        {
            if (v is JObject dict0 && dict0.ContainsKey(key))
            {
                dict = dict0;
                return true;
            }

            dict = null;
            return false;
        }


        /// <summary>Returns true if the given value is a JSON-LD value</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsValue(this JToken v)
        {
            return (v is JObject dict && dict.ContainsKey("@value"));
        }


        /// <summary>Returns true if the given value is a JSON-LD Array</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsArray(this JToken v)
        {
            return v is JArray;
        }

        /// <summary>Returns true if the given value is a JSON-LD List</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsList(this JToken v)
        {
            return v is JObject o && o.ContainsKey("@list");
        }

        /// <summary>Returns true if the given value is a JSON-LD Object</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsObject(this JToken v)
        {
            return v is JObject;
        }

        /// <summary>Returns true if the given value is a JSON-LD string</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsString(this JToken v)
        {
            return v.Type == JTokenType.String;
        }


        /// <summary>Returns true if the given value is a JSON-LD boolean</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsBool(this JToken v)
        {
            return v.Type == JTokenType.Boolean;
        }
        
        
        
        /// <summary>Returns true if the given value is a JSON-LD float</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsFloat(this JToken v)
        {
            return v.Type == JTokenType.Float;
        }
        
        /// <summary>Returns true if the given value is a JSON-LD Date</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsDate(this JToken v)
        {
            return v.Type == JTokenType.Date;
        }


        /// <summary>Returns true if the given value is a JSON-LD null object</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsNull(this JToken v)
        {
            return v == null || v.Type == JTokenType.Null;
        }


        /// <summary>Returns true if the given value is a blank node.</summary>
        public static bool IsBlankNode(JToken v)
        {
            // Note: A value is a blank node if all of these hold true:
            // 1. It is an Object.
            // 2. If it has an @id key its value begins with '_:'.
            // 3. It has no keys OR is not a @value, @set, or @list.
            if (!(v is JObject o)) return false;

            if (o.ContainsKey("@id"))
            {
                return ((string) o["@id"]).StartsWith("_:");
            }

            return o.Count == 0 || !(o.ContainsKey("@value") ||
                                     o.ContainsKey("@set") || o.ContainsKey("@list"));
        }

        /// <summary>
        /// Gives the value associated with the key
        /// </summary>
        /// <param name="v"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue">Return this value if nothing was found, throws an exception otherwise</param>
        /// <returns></returns>
        public static JToken GetContents(this JToken v, string key, JToken defaultValue = null)
        {
            if (v.IsArray())
            {
                // ReSharper disable once TailRecursiveCall
                return GetContents(v[0], key);
            }

            if (v.IsDictContaining(key, out var dict))
            {
                return dict[key];
            }

            if (defaultValue == null)
            {
                throw new ArgumentException($"Could not find {key}");
            }

            return defaultValue;
        }


        public static bool ArrayContains(this JArray array, string expected)
        {
            foreach (var elem in array)
            {
                if (elem.IsString() && elem.ToString().Equals(expected))
                {
                    return true;
                }
            }

            return false;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsType(this JObject json, string expectedType)
        {
            var typeT = json.GetContents("@type");
            return (typeT.IsString() && typeT.ToString().Equals(expectedType))
                   || ArrayContains((JArray) json.GetContents("@type"), expectedType);
        }

        /// <summary>
        /// Loads 'json["@type"]' (which should be a JArray) and
        /// checks that `expectedType` is one of the members of this array.
        ///
        /// If `expectedType` is not found, an exception is thrown.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public static void AssertTypeIs(this JObject json, params string[] expectedType)
        {
            var matchFound = false;
            foreach (var t in expectedType)
            {
                if (json.IsType(t))
                {
                    matchFound = true;
                    break;
                }
            }

            if (!matchFound && expectedType.Length == 1)
            {
                throw new ArgumentException(
                    $"The passed JSON does not follow the expected ontology. Expected type is {expectedType[0]}, known types are {json.GetContents("@type")}");
            }

            if (matchFound)
            {
                return;
            }

            var types = "";
            foreach (var t in expectedType)
            {
                types += t + ", ";
            }

            types = types.Substring(types.Length - 2);
            throw new ArgumentException(
                $"The passed JSON does not follow the expected ontology. Expected type is one of {types}, known types are {json.GetContents("@type")}");
        }

        public static string GetLdValue(this JToken json, string uriKey)
        {
            return json.GetContents(uriKey).GetLdValue();
        }

        public static string GetLdValue(this JToken json)
        {
            var value = json.GetContents("@value");
            if (value.IsFloat())
            {
                return ((float) value).ToString(CultureInfo.InvariantCulture);
            }
            return value.ToString();
        }

        public static string GetLdValue(this JToken json, string uriKey, string defaultValue)
        {
            return json.GetContents(uriKey, defaultValue).GetContents("@value", defaultValue).ToString();
        }


        public static int GetInt(this JToken json, string uriKey)
        {
            return int.Parse(json.GetLdValue(uriKey), CultureInfo.InvariantCulture);
        }

        public static int GetInt(this JToken json, string uriKey, int defaultValue)
        {
            return int.Parse(json.GetLdValue(uriKey, "" + defaultValue), CultureInfo.InvariantCulture);
        }

        public static float GetFloat(this JToken json, string uriKey)
        {
           // return float.Parse(json.GetLdValue(uriKey), CultureInfo.InvariantCulture);
           var value = json.GetContents(uriKey).GetContents("@value");
            if (value.IsFloat())
            {
                return (float) value;
            }

            return float.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        public static float GetFloat(this JToken json, string uriKey, float defaultValue)
        {
            // return float.Parse(json.GetLdValue(uriKey), CultureInfo.InvariantCulture);
            var defaultToken = JToken.Parse("{value: "+defaultValue.ToString(CultureInfo.InvariantCulture)+"}");
            defaultToken["@value"] = defaultValue;
            var value = json.GetContents(uriKey, defaultToken).GetContents("@value", defaultValue);
            if (value.IsFloat())
            {
                return (float) value;
            }

            return float.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }


        public static Uri GetId(this JToken json)
        {
            return new Uri(json.GetContents("@id").ToString());
        }

        public static Uri GetId(this JToken json, string uriKey)
        {
            return json.GetContents(uriKey).GetId();
        }


        public static DateTime GetDate(this JToken token, string uriKey)
        {
        
            // return float.Parse(json.GetLdValue(uriKey), CultureInfo.InvariantCulture);
            var value = token.GetContents(uriKey).GetContents("@value");
            if (value.IsDate())
            {
                return (DateTime) value;
            }

            return DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }
    }
}