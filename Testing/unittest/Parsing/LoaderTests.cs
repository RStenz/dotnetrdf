/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using Xunit;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Parsing
{

    public class LoaderTests
    {

        [Fact]
        public void ParsingDataUri1()
        {
            var rdfFragment = "@prefix : <http://example.org/> . :subject :predicate :object .";
            var rdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rdfFragment));
            var rdfAscii = Uri.EscapeDataString(rdfFragment);
            var uris = new List<string>()
            {
                "data:text/turtle;charset=UTF-8;base64," + rdfBase64,
                "data:text/turtle;base64," + rdfBase64,
                "data:;base64," + rdfBase64,
                "data:text/turtle;charset=UTF-8," + rdfAscii,
                "data:text/turtle," + rdfAscii,
                "data:," + rdfAscii
            };

            foreach (var uri in uris)
            {
                var u = new Uri(uri);

                Console.WriteLine("Testing URI " + u.AbsoluteUri);

                var g = new Graph();
                DataUriLoader.Load(g, u);

                Assert.Equal(1, g.Triples.Count);

                Console.WriteLine("Triples produced:");
                foreach (Triple t in g.Triples)
                {
                    Console.WriteLine(t.ToString());
                }
                Console.WriteLine();
            }
        }

        [Fact]
        public void ParsingDataUri2()
        {
            var rdfFragment = "@prefix : <http://example.org/> . :subject :predicate :object .";
            var rdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rdfFragment));
            var rdfAscii = Uri.EscapeDataString(rdfFragment);
            var uris = new List<string>()
            {
                "data:text/turtle;charset=UTF-8;base64," + rdfBase64,
                "data:text/turtle;base64," + rdfBase64,
                "data:;base64," + rdfBase64,
                "data:text/turtle;charset=UTF-8," + rdfAscii,
                "data:text/turtle," + rdfAscii,
                "data:," + rdfAscii
            };

            foreach (var uri in uris)
            {
                var u = new Uri(uri);

                Console.WriteLine("Testing URI " + u.AbsoluteUri);

                var g = new Graph();
                UriLoader.Load(g, u);

                Assert.Equal(1, g.Triples.Count);

                Console.WriteLine("Triples produced:");
                foreach (Triple t in g.Triples)
                {
                    Console.WriteLine(t.ToString());
                }
                Console.WriteLine();
            }
        }

        [SkippableFact]
        public void ParsingDBPedia()
        {
            Skip.IfNot(TestConfigManager.GetSettingAsBoolean(TestConfigManager.UseRemoteParsing), "Test Config marks Remote Parsing as unavailable, test cannot be run");

            var request = (HttpWebRequest)WebRequest.Create("http://dbpedia.org/resource/London");
            request.Accept = "application/rdf+xml";
            request.Method = "GET";
            request.Timeout = 45000;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Console.WriteLine("OK");
                    Console.WriteLine("Content Length: " + response.ContentLength);
                    Console.WriteLine("Content Type: " + response.ContentType);
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(webEx.Message);
                Console.WriteLine(webEx.StackTrace);
                Assert.True(false);
            }

            request = (HttpWebRequest)WebRequest.Create("http://dbpedia.org/data/London");
            request.Accept = "application/rdf+xml";
            request.Method = "GET";
            request.Timeout = 45000;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Console.WriteLine("OK");
                    Console.WriteLine("Content Length: " + response.ContentLength);
                    Console.WriteLine("Content Type: " + response.ContentType);
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(webEx.Message);
                Console.WriteLine(webEx.StackTrace);
                Assert.True(false);
            }

            try
            {
                var g = new Graph();
                UriLoader.Load(g, new Uri("http://dbpedia.org/resource/London"));
                Console.WriteLine("OK");
                Console.WriteLine(g.Triples.Count + " Triples retrieved");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Assert.True(false);
            }
        }

        private void SetUriLoaderCaching(bool cachingEnabled)
        {
            UriLoader.CacheEnabled = cachingEnabled;
        }

        [SkippableFact]
        public void ParsingUriLoaderDBPedia1()
        {
            Skip.IfNot(TestConfigManager.GetSettingAsBoolean(TestConfigManager.UseRemoteParsing), "Test Config marks Remote Parsing as unavailable, test cannot be run");

            var defaultTimeout = UriLoader.Timeout;
            try
            {
                SetUriLoaderCaching(false);
                UriLoader.Timeout = 45000;

                var g = new Graph();
                UriLoader.Load(g, new Uri("http://dbpedia.org/resource/Barack_Obama"));
                var formatter = new NTriplesFormatter();
                foreach (Triple t in g.Triples)
                {
                    Console.WriteLine(t.ToString(formatter));
                }
                Assert.False(g.IsEmpty, "Graph should not be empty");
            }
            finally
            {
                SetUriLoaderCaching(true);
                UriLoader.Timeout = defaultTimeout;
            }
        }

        [SkippableFact]
        public void ParsingUriLoaderDBPedia2()
        {
            Skip.IfNot(TestConfigManager.GetSettingAsBoolean(TestConfigManager.UseRemoteParsing), "Test Config marks Remote Parsing as unavailable, test cannot be run");

            IGraph g = new Graph();
            UriLoader.Load(g, new Uri("http://de.dbpedia.org/resource/Disillusion"));

            INodeFormatter formatter = new TurtleW3CFormatter();
            foreach (INode p in g.Triples.Select(t => t.Predicate).Distinct())
            {
                Console.WriteLine("ToString() = " + p.ToString());
                Console.WriteLine("Formatted = " + p.ToString(formatter));
                Console.WriteLine("URI ToString() = " + ((IUriNode)p).Uri.ToString());
            }
        }

        [SkippableFact]
        public void ParsingUriLoaderDBPedia3()
        {
            Skip.IfNot(TestConfigManager.GetSettingAsBoolean(TestConfigManager.UseRemoteParsing), "Test Config marks Remote Parsing as unavailable, test cannot be run");

            var defaultTimeout = UriLoader.Timeout;
            try
            {
                SetUriLoaderCaching(false);
                UriLoader.Timeout = 45000;

                var g = new Graph();
                UriLoader.Load(g, new Uri("http://dbpedia.org/ontology/wikiPageRedirects"), new RdfXmlParser());
                Assert.False(g.IsEmpty, "Graph should not be empty");
                TestTools.ShowGraph(g);
            }
            finally
            {
                SetUriLoaderCaching(true);
                UriLoader.Timeout = defaultTimeout;
            }
        }

        [Fact]
        public void ParsingEmbeddedResourceInDotNetRdf()
        {
            var g = new Graph();
            EmbeddedResourceLoader.Load(g, "VDS.RDF.Configuration.configuration.ttl");

            TestTools.ShowGraph(g);

            Assert.False(g.IsEmpty, "Graph should be non-empty");
        }

        [Fact]
        public void ParsingEmbeddedResourceInDotNetRdf2()
        {
            var g = new Graph();
            EmbeddedResourceLoader.Load(g, "VDS.RDF.Configuration.configuration.ttl, dotNetRDF");

            TestTools.ShowGraph(g);

            Assert.False(g.IsEmpty, "Graph should be non-empty");
        }

        [Fact]
        public void ParsingEmbeddedResourceInExternalAssembly()
        {
            var g = new Graph();
            EmbeddedResourceLoader.Load(g, "VDS.RDF.embedded.ttl, dotNetRDF.Test");

            TestTools.ShowGraph(g);
            Assert.False(g.IsEmpty, "Graph should be non-empty");
        }

        [Fact]
        public void ParsingEmbeddedResourceLoaderGraphIntoTripleStore()
        {
            var store = new TripleStore();
            store.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            Assert.True(store.Triples.Count() > 0);
            Assert.Equal(1, store.Graphs.Count);
        }

        [Fact]
        public void ParsingFileLoaderGraphIntoTripleStore()
        {
            var g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
            g.SaveToFile("fileloader-graph-to-store.ttl");

            var store = new TripleStore();
            
            store.LoadFromFile("fileloader-graph-to-store.ttl");

            Assert.True(store.Triples.Count() > 0);
            Assert.Equal(1, store.Graphs.Count);
        }
       
    }
}
