using System.Collections.Generic;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Util
{
    public class Obj
    {
        public static bool Contains(object map, params string[] keys)
        {
            foreach (string key in keys)
            {
                map = ((IDictionary<string, JToken>)map)[key];
                if (map == null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>A null-safe equals check using v1.equals(v2) if they are both not null.</summary>
        /// <remarks>A null-safe equals check using v1.equals(v2) if they are both not null.</remarks>
        /// <param name="v1">The source object for the equals check.</param>
        /// <param name="v2">
        /// The object to be checked for equality using the first objects
        /// equals method.
        /// </param>
        /// <returns>
        /// True if the objects were both null. True if both objects were not
        /// null and v1.equals(v2). False otherwise.
        /// </returns>
        public new static bool Equals(object v1, object v2)
        {
            return v1?.Equals(v2) ?? v2 == null;
        }
    }
}
