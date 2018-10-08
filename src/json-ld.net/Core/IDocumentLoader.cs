using System;
using System.Collections;
using System.IO;
using System.Linq;
using JsonLD.Core;
using JsonLD.Util;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    /// <summary>
    /// An interface representing some object that downloads code.
    /// Actual downloading is _not_ handled by this library and should be injected
    /// </summary>
    public interface IDocumentLoader
    {
        JToken LoadDocument(Uri uri);
    }

    public class HttpDocumentDownloader : IDocumentLoader
    {
        private readonly HttpClient _client;
        public readonly List<Uri> DownloadedDocuments = new List<Uri>();

        public HttpDocumentDownloader(HttpClient client)
        {
            _client = client;
        }


        public HttpDocumentDownloader() : this (new HttpClient())
        {
            
        }

        public JToken LoadDocument(Uri uri)
        {
            DownloadedDocuments.Add(uri);
            // Data as string
            var data = _client.GetAsync(uri).Synced().Content.ReadAsStringAsync().Synced();
            return JObject.Parse(data);
        }
    }
}