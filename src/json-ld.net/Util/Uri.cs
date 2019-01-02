using System;

namespace json_ld.net.Util
{
    public static class UriExtensions
    {
        public static Uri RemoveLastSegment(this Uri uri)
        {
            var noLastSegment = $"{uri.Scheme}://{uri.Authority}";

            for (int i = 0; i < uri.Segments.Length - 1; i++)
            {
                noLastSegment += uri.Segments[i];
            }

            return new Uri(noLastSegment);
        }

    }
}