using System.Collections;
using System.IO;
using JsonLD.Util;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;

namespace JsonLD.Util
{
    /// <summary>A bunch of functions to make loading JSON easy</summary>
    /// <author>tristan</author>
    public static class JsonUtils
    {
        public static string ToPrettyString(JToken obj)
        {
            StringWriter sw = new StringWriter();
            var serializer = new JsonSerializer();
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, obj);
            }
            return sw.ToString();
        }
    }
}
