using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

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
        /// <param name="dict">The typecasted dictionary will be saved in this variable</param>
        /// <returns></returns>
        internal static bool IsDictContaining(this JToken v, string key, out JObject dict)
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
        internal static bool IsValue(this JToken v)
        {
            return (v is JObject dict && dict.ContainsKey($"@value"));
        }


        /// <summary>Returns true if the given value is a JSON-LD Array</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsArray(this JToken v)
        {
            return v is JArray;
        }

        /// <summary>Returns true if the given value is a JSON-LD List</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsList(this JToken v)
        {
            return v is JObject && ((IDictionary<string, JToken>) v).ContainsKey("@list");
        }

        /// <summary>Returns true if the given value is a JSON-LD Object</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsObject(this JToken v)
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

        /// <summary>Returns true if the given value is a JSON-LD null object</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        public static bool IsNull(this JToken v)
        {
            return v == null ||  v.Type == JTokenType.Null;
        }
        

        /// <summary>Returns true if the given value is a blank node.</summary>
        internal static bool IsBlankNode(JToken v)
        {
            // Note: A value is a blank node if all of these hold true:
            // 1. It is an Object.
            // 2. If it has an @id key its value begins with '_:'.
            // 3. It has no keys OR is not a @value, @set, or @list.
            if (!(v is JObject o)) return false;
            
            if (o.ContainsKey("@id"))
            {
                return ((string)o["@id"]).StartsWith("_:");
            }

            return o.Count == 0 || !(o.ContainsKey("@value") ||
                                     o.ContainsKey("@set") || o.ContainsKey("@list"));
        }
        
        /// <summary>
        /// Gives the value associated with the key
        /// </summary>
        /// <param name="v"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static JToken GetContents(this JToken v, string key)
        {
            return ((JObject) v)[key];
        }
    }
}