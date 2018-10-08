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

There are two directories in the src-path: `Json-Ld` and `RDF`. One contains stuff concerning JSON-LD, the other is the RDF framework. At the moment, the RDF-part is unsupported and neglected.

### Json-Ld/


#### JSon-Ld

As the library is about JSON-LD, there is an class representing the JSON-LD data structure. However, there is something peculiar about that class: it does _not_ define a JSON-LD object. Quite the opposite: `JObject` from the standard libraries is reused to represent Json-LD. However, `JsonLd.cs` is the home of many extension methods making programming with JSON-LD easier. Although no important algorithms are defined there, a lot of utils and vocabulary is introduced there, so have a look to familiarize yourself with the terms.


#### Context

The second most important piece of the library is `Context.cs`. This class represents a context and offers all the necessary data to perform algorithms such as IRI (an IRI is a fancy upgrade of URI/URL) expansion and compactions, ...

The algorithms themselves have their home in `ContextAlgos` - by seperating the algorithms and the data definition, one can quickly see what data is saved in the context without having to dig through 1000+ lines of algorithms.

#### JsonLd-Processor

The Json-LD processor is the main entry point for the user. The user a processor object and asks it to modify a JObject. `JsonLdProcessor.cs` is merely a facade, delegating work to the actual algorithms (defined in ProcessorAlgos).

Note that a JsonLdProcessor is specialized on a single host (via the LD-OPtions). In other words, you'll need a single processor for each host you work with.


#### JsonLd-options

A small class keeping track of the desired options, containing flags such as 'compact arrays or not', what is the base URI we're working with, ...

#### JsonLd-errors

Constants to keep track of error codes.

RDF
---

The RDF-directory contains mainly classes to convert JSON-LD into other semantical formats (such as triples). At the moment, this is out of scope. The code here is neglected and probably doesn't function.

Utils
-----

Utils is like the attic: lots of stuff that either deserves a better place in the code base or should be thrown out altogether. It is legacy code from the original fork, although mainly untouched.



