using System;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
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
        // An example use case
        public void TestSimpleUseCase()
        {
            // If you want to provide your own http-client, use
            // new HttpDocumentDownloader(yourHttpClient);
            // For more fancyness, implement the single method of IDocumentDownloader
            var loader = new HttpDocumentDownloader();

            var options = new JsonLdOptions("http://graph.irail.be")
                {Explicit = false, Embed = true, };
            var processor = new JsonLdProcessor(loader, options);

            var loaded = processor.Load(new Uri("http://graph.irail.be/sncb/connections"));
            var ctx = processor.ExtractContext(loaded["@context"]);
            var expanded = processor.Expand(ctx, loaded);
            Log(ctx.Serialize().ToString());
            //  Log(loaded.ToString());
            foreach (var uri in loader.DownloadedDocuments)
            {
                Log($"Downloaded: {uri}");
            }

            var compacted = processor.Compact(ctx, expanded);
            Log(compacted.ToString());
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}