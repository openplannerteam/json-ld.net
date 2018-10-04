﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;
using System.IO;
using Newtonsoft.Json;
using JsonLD.Core;
using JsonLD.Util;

namespace JsonLD.Test
{
    public class ConformanceTests
    {
        [Theory, ClassData(typeof(ConformanceCases))]
        public void ConformanceTestPasses(string id, string testname, ConformanceCase conformanceCase)
        {
            JToken result = conformanceCase.run();
            if (conformanceCase.error != null)
            {
                Assert.True(((string) result["error"]).StartsWith((string) conformanceCase.error),
                    "Resulting error doesn't match expectations.");
            }
            else
            {
                if (!JsonLdUtils.DeepCompare(result, conformanceCase.output))
                {
#if DEBUG
                    Console.WriteLine(id);
                    Console.WriteLine("Actual:");
                    Console.Write(JsonUtils.ToPrettyString(result));
                    Console.WriteLine("--------------------------");
                    Console.WriteLine("Expected:");
                    Console.Write(JsonUtils.ToPrettyString(conformanceCase.output));
                    Console.WriteLine("--------------------------");
#endif

                    Assert.True(false, "Returned JSON doesn't match expectations.");
                }
            }
        }
    }

    public class ConformanceCase
    {
        public JToken input { get; set; }
        public JToken context { get; set; }
        public JToken frame { get; set; }
        public JToken output { get; set; }
        public JToken error { get; set; }
        public Func<JToken> run { get; set; }
    }

    public class ConformanceCases : IEnumerable<object[]>
    {
        string[] manifests = new[]
        {
            "compact-manifest.jsonld",
            "expand-manifest.jsonld",
            "flatten-manifest.jsonld",
            "frame-manifest.jsonld",
            "toRdf-manifest.jsonld",
            "fromRdf-manifest.jsonld",
            "normalize-manifest.jsonld",
// Test tests are not supported on CORE CLR
#if !PORTABLE && !IS_CORECLR
            "error-manifest.jsonld",
            "remote-doc-manifest.jsonld",
#endif
        };

        public ConformanceCases()
        {
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (string manifest in manifests)
            {
                JToken manifestJson;

                manifestJson = GetJson(manifest);

                foreach (JObject testcase in manifestJson["sequence"])
                {
                    Func<JToken> run;
                    ConformanceCase newCase = new ConformanceCase();

                    newCase.input = GetJson(testcase["input"]);
                    newCase.context = GetJson(testcase["context"]);
                    newCase.frame = GetJson(testcase["frame"]);

                    var options =
                        new JsonLdOptions("http://json-ld.org/test-suite/tests/" + (string) testcase["input"]);

                    var testType = (JArray) testcase["@type"];

                    if (testType.Any((s) => (string) s == "jld:NegativeEvaluationTest"))
                    {
                        newCase.error = testcase["expect"];
                    }
                    else if (testType.Any((s) => (string) s == "jld:PositiveEvaluationTest"))
                    {
                        if (testType.Any((s) =>
                            new List<string> {"jld:ToRDFTest", "jld:NormalizeTest"}.Contains((string) s)))
                        {
                            newCase.output = File.ReadAllText(Path.Combine("W3C", (string) testcase["expect"]));
                        }
                        else if (testType.Any((s) => (string) s == "jld:FromRDFTest"))
                        {
                            newCase.input = File.ReadAllText(Path.Combine("W3C", (string) testcase["input"]));
                            newCase.output = GetJson(testcase["expect"]);
                        }
                        else
                        {
                            newCase.output = GetJson(testcase["expect"]);
                        }
                    }
                    else
                    {
                        throw new Exception("Expecting either positive or negative evaluation test.");
                    }

                    JToken optionToken;
                    JToken value;

                    if (testcase.TryGetValue("option", out optionToken))
                    {
                        JObject optionDescription = (JObject) optionToken;

                        if (optionDescription.TryGetValue("compactArrays", out value))
                        {
                            options.CompactArrays = (bool) value;
                        }

                        if (optionDescription.TryGetValue("base", out value))
                        {
                            options.Base = (string) value;
                        }

                        if (optionDescription.TryGetValue("expandContext", out value))
                        {
                            newCase.context = GetJson(testcase["option"]["expandContext"]);
                            options.ExpandContext = (JObject) newCase.context;
                        }

                        if (optionDescription.TryGetValue("produceGeneralizedRdf", out value))
                        {
                            options.ProduceGeneralizedRdf = (bool) value;
                        }

                        if (optionDescription.TryGetValue("useNativeTypes", out value))

                            options.UseNativeTypes = (bool) value;
                    }

                    if (optionDescription.TryGetValue("useRdfType", out value))
                    {
                        options.UseRdfType = (bool) value;
                    }
                }

                if (testType.Any((s) => (string) s == "jld:CompactTest"))
                {
                    run = () => JsonLdProcessor.Compact(newCase.input, newCase.context, options);
                }
                else if (testType.Any((s) => (string) s == "jld:ExpandTest"))
                {
                    run = () => JsonLdProcessor.Expand(newCase.input, options);
                }
                else if (testType.Any((s) => (string) s == "jld:FlattenTest"))
                {
                    run = () => JsonLdProcessor.Flatten(newCase.input, newCase.context, options);
                }
                else if (testType.Any((s) => (string) s == "jld:FrameTest"))
                {
                    run = () => JsonLdProcessor.Frame(newCase.input, newCase.frame, options);
                }
                else if (testType.Any((s) => (string) s == "jld:NormalizeTest"))
                {
                    run = () => new JValue(
                        RDFDatasetUtils.ToNQuads((RDFDataset) JsonLdProcessor.Normalize(newCase.input, options))
                            .Replace("\n", "\r\n")
                    );
                }
                else if (testType.Any((s) => (string) s == "jld:ToRDFTest"))
                {
                    options.format = "application/nquads";
                    run = () => new JValue(
                        ((string) JsonLdProcessor.ToRDF(newCase.input, options)).Replace("\n", "\r\n")
                    );
                }
                else if (testType.Any((s) => (string) s == "jld:FromRDFTest"))
                {
                    options.format = "application/nquads";
                    run = () => JsonLdProcessor.FromRDF(newCase.input, options);
                }
                else
                {
                    run = () => { throw new Exception("Couldn't find a test type, apparently."); };
                }

                if ((string) manifestJson["name"] == "Remote document")
                {
                    Func<JToken> innerRun = run;
                    run = () =>
                    {
                        var remoteDoc =
                            options.documentLoader.LoadDocument(
                                "https://json-ld.org/test-suite/tests/" + (string) testcase["input"]);
                        newCase.input = remoteDoc.Document;
                        options.SetBase(remoteDoc.DocumentUrl);
                        options.SetExpandContext((JObject) remoteDoc.Context);
                        return innerRun();
                    };
                }

                if (testType.Any((s) => (string) s == "jld:NegativeEvaluationTest"))
                {
                    Func<JToken> innerRun = run;
                    run = () =>
                    {
                        try
                        {
                            return innerRun();
                        }
                        catch (JsonLdError err)
                        {
                            JObject result = new JObject();
                            result["error"] = err.Message;
                            return result;
                        }
                    };
                }

                newCase.run = run;

                yield return new object[] {manifest + (string) testcase["@id"], (string) testcase["name"], newCase};
            }
        }
    }

    private JToken GetJson(JToken j)
    {
    try
    {
    if (j == null || j.Type == JTokenType.Null) return null;
    using (Stream manifestStream = File.OpenRead(Path.Combine("W3C", (string)j)))
    using (TextReader reader = new StreamReader(manifestStream))
    using (JsonReader jreader = new Newtonsoft.Json.JsonTextReader(reader)
    {
    DateParseHandling = DateParseHandling.None
    })
    {
    return JToken.ReadFrom(jreader);
}

}
catch (Exception e)
{
// TODO: this should not be here, figure out why this is needed or catch specific exception.
return null;
}
}
System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
{
throw new Exception("auggh");
}
}
}