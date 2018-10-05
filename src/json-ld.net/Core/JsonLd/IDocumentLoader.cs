using System;
using System.Collections;
using System.IO;
using System.Linq;
using JsonLD.Core;
using JsonLD.Util;
using System.Net;
using System.Collections.Generic;

namespace JsonLD.Core
{
    /// <summary>
    /// An interface representing some object that downloads code.
    /// Actual downloading is _not_ handled by this library and should be injected
    /// </summary>
    public interface IDocumentLoader
    {
        RemoteDocument LoadDocument(Uri uri);
    }
}
