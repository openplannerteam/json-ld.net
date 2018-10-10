json-ld.net
==========

A JSON-LD processor for .NET, heavily reworked.

Scope
=====

For now, this library supports compaction and expansion of JSON-LD documents using a context object. Transforming to RDF is not (yet) there.

Providing network access is seen as a responsibility of the user of this library. As user, you have to give this library an object that can perform loading of remote documents - giving you control over caching, local documents, ... 

For people not needing that much control, a default downloader is provided.

Usage
=====

Vanilla usage
-------------

If you want to use this library, first start with creating an `IDocumentLoader`. One default is provided, namely  `HttpDocumentLoader` which serves as a wrapper around `HttpClient`.


`
var httpClient = new HttpClient();
var loader = new HttpDocumentLoader(httpClient);
`

With this loader, you can create a JsonLdProcessor. Note that a single Processor is tied to a single basepath (e.g. `http://graph.irail.be`) which it'll use to expand or compact the JSON against. This basepath should be passed along:

`
var processor = new JsonLdProcessor(loader, new Uri("http://graph.irail.be"))
`

The processor is now ready for usage. To download a document in fully expanded format, request it from the processor:

`
var data = processor.LoadExpanded(new Uri("http://graph.irail.be/sncb/connections"));
`

Advanced usage
--------------

If you want more control over document retrieval via the internet (e.g. to control caching headers, ...), implement `IDocumentLoader` yourself - you only need to implement one single method for this: `LoadDocument(Uri)`.

If you want control over the context, expansion and formatting, use

`
var data = processor.Load(URI); // Note that processor.Load is but a wrapper around the document loader. Nothing fancy happens here
var context = processor.extractContext(data);
var expanded = processor.Expand(context, data);
processor.Compact(context, expanded);
`


Overview
========

For an overview of the files, have a loot at [Overview.md]


History
======= 

This project began life as a [Sharpen][sharpen]-based auto-port from [jsonld-java][jsonld-java].

  [sharpen]: http://community.versant.com/Projects/html/projectspaces/db4o_product_design/sharpen.html
  [jsonld-java]: https://github.com/jsonld-java/jsonld-java

It subsequently has been forked of by pietervdvn, as member of the smart mobility call for usage within the 'Itinero-Transit'-module.
It went through a major refactoring.

