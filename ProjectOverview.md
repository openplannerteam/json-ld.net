Project Overview
================


This document is intended for persons whom want to start developing on JSON-LD.net, using the Itinero fork.

When I (pietervdvn) started working with the library, in order to see the links and bigger structure, I refactored a lot, threw away some code and fixed Jetbrains Rider-warnings. This document details what ended where 

Getting started
---------------

First of all, you'll need to know something about linked data as this library is all about that. Have a look at https://rubenverborgh.github.io/WebFundamentals/


Scope
-----

This library is responsible for parsing JSON-LD objects and asking for retrieval of remote contexts. However, providing network access is the responsiblity of the client - when using the library, the user passes an object `IDocumentLoader` which is responsible for retrieving the right documents when needed. This way, the user is free to retrieve the needed documents from the internet, from cache, let them be generated, ...

Fundamental classes
-------------------

There are two directories in the src-path: `Json-Ld` and `RDF`. One contains stuff concerning JSON-LD, the other is the RDF framework.

### JSon-Ld

As the library is about JSON-LD, there is an class representing the JSON-LD data structure. However, there is something peculiar about that class: it does _not_ define a JSON-LD object. Quite the opposite: `JObject` from the standard libraries is reused. `JsonLd.cs` is the home of many extension methods making life easier. Although no important algorithms are defined there, a lot of utils and vocabulary is introduced there, so have a look.


### Context

The second most important piece of the library is `Context.cs`. This class represents a context and offers all the necessary data to perform algorithms such as IRI (an IRI is a fancy upgrade of URI/URL) expansion and compactions, ...

The algorithms themselves have their home in `ContextAlgos` - by seperating the algorithms and the data definition, one can quickly see what data is saved in the context without having to dig through 1000+ lines of algorithms.
