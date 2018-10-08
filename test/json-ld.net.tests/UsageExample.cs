using System;
using System.Net.Http;
using JsonLD.Core;
using Xunit;
using Xunit.Abstractions;

namespace JsonLD.Test
{
    public class UsageExample
    {
        private readonly ITestOutputHelper _output;

        public UsageExample(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestStorageLocation()
        {
            // If you want to provide your own http-client, use
            // new HttpDocumentDownloader(yourHttpClient);
            // For more fancyness, implment the single method of IDocumentDownloader
            var loader = new HttpDocumentDownloader();

            var processor = new JsonLdProcessor(loader);

            var loaded = processor.Load(new Uri("http://graph.irail.be/sncb/connections"));
            Log(loaded.ToString());
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}