using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace JsonLD.Test.Core.JsonLd
{
    public class JsonLdTest
    {


        [Fact]
        public void TestFloatParsingAndSearching()
        {
            var token1 = JToken.Parse("{ a : {value: 1.5}  }");
            var token2 = JToken.Parse("{ a : {value: \"1.5\"}  }");

            token1["a"]["@value"] = token1["a"]["value"];
            token2["a"]["@value"] = token2["a"]["value"];
            
            
            var f1 = token1.GetFloat("a");
            
            Assert.Equal(1.5, f1);
            
            var f2 = token2.GetFloat("a");

            Assert.Equal(1.5, f2);


            var f3 = token1.GetFloat("b", (float) 2.5);
            Assert.Equal(2.5, f3);
            var f4 = token1.GetFloat("a", (float) 2.5);
            Assert.Equal(1.5, f4);

        }
        
    }
}