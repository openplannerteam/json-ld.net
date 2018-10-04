using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    public static class JsonLdUtils2
    {
        /// <summary>Returns true if the given value is a JSON-LD value</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsDictContaining(this JToken v, string key)
        {
            return v is JObject dict && dict.ContainsKey(key);
        }

        internal static JToken GetContents(this JToken v, string key)
        {
            return ((JObject) v)[key];
        }
    }
}