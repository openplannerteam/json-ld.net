using System;
using Newtonsoft.Json.Linq;
using JsonLD.Core.ContextAlgos;

namespace JsonLD.Core
{
    /// <summary>
    ///
    /// The 'Context'-part of a JSON-LD document gives some common terms and definitions which are used in the
    /// latter of the document to refer efficiently to terms.
    ///
    /// In other words, the context enables some compression by giving a common dictionary.
    ///
    /// For example, the JSON may be
    ///
    /// {
    ///     "@context": {
    ///         "name": "http://schema.org/name",
    ///         "image": {
    ///             "@id": "http://schema.org/image",
    ///             "@type": "@id"
    ///         },
    ///         "homepage": {
    ///             "@id": "http://schema.org/url",
    ///             "@type": "@id"
    ///         }
    ///     },
    ///     "name": "Mano Sporty",
    ///     "homepage": "http://manu.sporny.org/",
    ///     "image": "http://manu.sporny.org/images/manu.png"
    /// }
    ///
    /// This would be equivalent to writing:
    ///
    ///     /// {
    ///     "@context": {
    ///         "name": "http://schema.org/name",
    ///         "image": {
    ///             "@id": "http://schema.org/image",
    ///             "@type": "@id"
    ///         },
    ///         "homepage": {
    ///             "@id": "http://schema.org/url",
    ///             "@type": "@id"
    ///         }
    ///     },
    ///     "http://schema.org/name": "Mano Sporty",
    ///     "http://schema.org/image": {"@id": "http://manu.sporny.org/"},
    ///     "http://schema.org/url": {@id: "http://manu.sporny.org/images/manu.png"}
    /// }
    ///
    ///
    /// 
    /// Also see: https://json-ld.org/spec/FCGS/json-ld/20180607/#the-context
    /// The above examples come from the latter website as well.
    ///
    /// This class contains the data structures and minimal support functions for the context.
    /// More advanced algorithms (expansion, compaction, ...) can be found in ContextAlgos
    ///
    /// 
    /// </summary>
    public class Context : JObject
    {
        public JsonLdOptions Options;

        public JObject TermDefinitions = new JObject();

        /// <summary>
        /// Memoization of the Inverse object, will be calculated the first time it is needed
        /// </summary>
        private JObject _inverse;

        public Context(JsonLdOptions options)
        {
            Init(options);
        }

        private Context(JToken context, JsonLdOptions opts) : base(context as JObject)
        {
            Init(opts);
        }


        // TODO: load remote context
        private void Init(JsonLdOptions options)
        {
            Options = options;
            if (!string.IsNullOrEmpty(options.Base))
            {
                this["@base"] = options.Base;
            }
        }


        public Context Clone()
        {
            return new Context(DeepClone(), Options)
                {TermDefinitions = (JObject) TermDefinitions.DeepClone()};
        }

        /// <summary>
        /// Returns an inverse context for this context. Might create a new one if the context hasn't been built yet
        /// </summary>
        public JObject GetInverse()
        {
            // lazily create inverse
            if (_inverse != null)
            {
                return _inverse;
            }

            _inverse = this.CreateInverse();
            return _inverse;
        }
   
        /// <summary>Retrieve container mapping.</summary>
        public string GetContainer(string property)
        {
            if (property == null)
            {
                return null;
            }

            if ("@graph".Equals(property))
            {
                return "@set";
            }

            if (JsonLd.IsKeyword(property))
            {
                return property;
            }

            var td = (JObject) TermDefinitions[property];

            if (td == null)
            {
                return null;
            }

            return (string) td["@container"];
        }

        public virtual bool IsReverseProperty(string property)
        {
            if (property == null)
            {
                return false;
            }

            var td = (JObject) TermDefinitions[property];
            if (td == null)
            {
                return false;
            }

            var reverse = td["@reverse"];
            return !reverse.IsNull() && (bool) reverse;
        }


        public string GetTypeMapping(string property)
        {
            return (string) GetMapping(property, "@type");
        }

        public string GetLanguageMapping(string property)
        {
            return (string) GetMapping(property, "@language");
        }

        internal virtual JObject GetTermDefinition(string key)
        {
            return (JObject) TermDefinitions[key];
        }
        
        private JToken GetMapping(string property, string key)
        {
            if (property == null)
            {
                return null;
            }

            var td = TermDefinitions[key];
            return ((JObject) td)?[key];
        }

        
        public JObject Serialize()
        {
            return ContextSerialization.Serialize(this);
        }
    }
}