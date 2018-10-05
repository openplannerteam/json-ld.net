using System;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    /// <summary>
    /// Contains fixed error strings.
    /// Pretty boring.
    /// </summary>
    public class JsonLdError : Exception
    {
        private Error _type;
        private readonly JObject details = null;

        public JsonLdError(Error type, object detail) :
            base(detail == null? string.Empty
              : detail.ToString())
        {
            _type = type;
        }

        public JsonLdError(Error type) : base(string.Empty)
        {
            _type = type;
        }

        public sealed class Error
        {
            public static readonly Error InvalidVersion = new Error("Version not supported");

            public static readonly Error LoadingDocumentFailed = new Error("loading document failed");

            public static readonly Error ListOfLists = new Error("list of lists");

            public static readonly Error InvalidIndexValue = new Error("invalid @index value");

            public static readonly Error ConflictingIndexes = new Error("conflicting indexes");

            public static readonly Error InvalidIdValue = new Error("invalid @id value");

            public static readonly Error InvalidLocalContext = new Error("invalid local context");

            public static readonly Error MultipleContextLinkHeaders = new Error("multiple context link headers");

            public static readonly Error LoadingRemoteContextFailed = new Error("loading remote context failed");

            public static readonly Error InvalidRemoteContext = new Error("invalid remote context");

            public static readonly Error RecursiveContextInclusion = new Error("recursive context inclusion");

            public static readonly Error InvalidBaseIri = new Error("invalid base IRI");

            public static readonly Error InvalidVocabMapping = new Error("invalid vocab mapping");

            public static readonly Error InvalidDefaultLanguage = new Error("invalid default language");

            public static readonly Error KeywordRedefinition = new Error("keyword redefinition");

            public static readonly Error InvalidTermDefinition = new Error("invalid term definition");

            public static readonly Error InvalidReverseProperty = new Error("invalid reverse property");

            public static readonly Error InvalidIriMapping = new Error("invalid IRI mapping");

            public static readonly Error CyclicIriMapping = new Error("cyclic IRI mapping");

            public static readonly Error InvalidKeywordAlias = new Error("invalid keyword alias");

            public static readonly Error InvalidTypeMapping = new Error("invalid type mapping");

            public static readonly Error InvalidLanguageMapping = new Error("invalid language mapping");

            public static readonly Error CollidingKeywords = new Error("colliding keywords");

            public static readonly Error InvalidContainerMapping = new Error("invalid container mapping");

            public static readonly Error InvalidTypeValue = new Error("invalid type value");

            public static readonly Error InvalidValueObject = new Error("invalid value object");

            public static readonly Error InvalidValueObjectValue = new Error("invalid value object value");

            public static readonly Error InvalidLanguageTaggedString = new Error("invalid language-tagged string");

            public static readonly Error InvalidLanguageTaggedValue = new Error("invalid language-tagged value");

            public static readonly Error InvalidTypedValue = new Error("invalid typed value");

            public static readonly Error InvalidSetOrListObject = new Error("invalid set or list object");

            public static readonly Error InvalidLanguageMapValue = new Error("invalid language map value");

            public static readonly Error CompactionToListOfLists = new Error("compaction to list of lists");

            public static readonly Error InvalidReversePropertyMap = new Error("invalid reverse property map");

            public static readonly Error InvalidReverseValue = new Error("invalid @reverse value");

            public static readonly Error InvalidReversePropertyValue = new Error("invalid reverse property value");

            public static readonly Error SyntaxError = new Error("syntax error");

            public static readonly Error NotImplemented = new Error("not implemented");

            public static readonly Error UnknownFormat = new Error("unknown format");

            public static readonly Error InvalidInput = new Error("invalid input");

            public static readonly Error ParseError = new Error("parse error");

            public static readonly Error UnknownError = new Error("unknown error");

            private readonly string error;

            private Error(string error)
            {
                // non spec related errors
                this.error = error;
            }

            public override string ToString()
            {
                return error;
            }
        }

        public new virtual Error GetType()
        {
            return _type;
        }

        public override string Message
        {
            get
            {
                string msg = base.Message;
                if (msg != null && !string.Empty.Equals(msg))
                {
                    return _type + ": " + msg;
                }

                return _type.ToString();
            }
        }
    }
}