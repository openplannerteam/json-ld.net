using System;
using JsonLD.Core;
using Xunit;
using Xunit.Abstractions;

namespace JsonLD.Test
{
    public class SNCBTest
    {
        private readonly ITestOutputHelper _output;

        public SNCBTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SncbTest()
        {
            // If you want to provide your own http-client, use
            // new HttpDocumentDownloader(yourHttpClient);
            // For more fancyness, implement the single method of IDocumentDownloader
            var loader = new HttpDocumentDownloader();

            var options = new JsonLdOptions("http://graph.irail.be")
                {Explicit = false, Embed = true, };
            var processor = new JsonLdProcessor(loader, options);

            var loaded = processor.LoadExpanded(new Uri("https://graph.irail.be/sncb/connections"));
            Log(loaded.ToString());
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}