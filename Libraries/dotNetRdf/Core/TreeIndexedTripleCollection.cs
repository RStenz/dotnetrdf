/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
// 
// Copyright (c) 2009-2021 dotNetRDF Project (http://dotnetrdf.org/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

using System.Collections.Generic;
using System.Linq;
using VDS.Common.Collections;
using VDS.Common.Trees;

namespace VDS.RDF
{
    /// <summary>
    /// An indexed triple collection that uses our <see cref="MultiDictionary{TKey,TValue}"/> and <see cref="BinaryTree{TKey,TValue}"/> implementations under the hood for the index structures.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class TreeIndexedTripleCollection
        : AbstractIndexedTripleCollection
    {
        // Simple Indexes
        private readonly MultiDictionary<INode, HashSet<Triple>> _s, _p, _o;
        // Compound Indexes
        private readonly MultiDictionary<Triple, HashSet<Triple>> _sp, _so, _po;

        // Placeholder Variables for compound lookups
        private readonly VariableNode _subjVar = new VariableNode("s"),
                             _predVar = new VariableNode("p"),
                             _objVar = new VariableNode("o");


        /// <summary>
        /// Creates a new Tree Indexed triple collection.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public TreeIndexedTripleCollection() : this(Options.FullTripleIndexing /* true */)
#pragma warning restore CS0618 // Type or member is obsolete
        {
        }

        /// <summary>
        /// Creates a new Tree Indexed triple collection.
        /// </summary>
        /// <param name="fullTripleIndexing">Whether to create subject-predicate, subject-object and subject-predicate indexes in addition to the basic subject, predicate and object indexes. Defaults to true.</param>
        public TreeIndexedTripleCollection(bool fullTripleIndexing)
             : this(MultiDictionaryMode.AVL, fullTripleIndexing) { }

        /// <summary>
        /// Creates a new Tree Indexed triple collection.
        /// </summary>
        /// <param name="compoundIndexMode">Mode to use for compound indexes.</param>
        /// <param name="fullTripleIndexing">Whether to create subject-predicate, subject-object and subject-predicate indexes in addition to the basic subject, predicate and object indexes. Defaults to true.</param>
        public TreeIndexedTripleCollection(MultiDictionaryMode compoundIndexMode, bool fullTripleIndexing = true)
            : this(true, true, true, fullTripleIndexing, fullTripleIndexing, fullTripleIndexing, compoundIndexMode) { }

        /// <summary>
        /// Creates a new Tree Indexed triple collection with the given Indexing options.
        /// </summary>
        /// <param name="subjIndex">Whether to create a subject index.</param>
        /// <param name="predIndex">Whether to create a predicate index.</param>
        /// <param name="objIndex">Whether to create an object index.</param>
        /// <param name="subjPredIndex">Whether to create a subject predicate index.</param>
        /// <param name="subjObjIndex">Whether to create a subject object index.</param>
        /// <param name="predObjIndex">Whether to create a predicate object index.</param>
        /// <param name="compoundIndexMode">Mode to use for compound indexes.</param>
        public TreeIndexedTripleCollection(bool subjIndex, bool predIndex, bool objIndex, bool subjPredIndex, bool subjObjIndex, bool predObjIndex, MultiDictionaryMode compoundIndexMode)
        {
            if (subjIndex) _s = new MultiDictionary<INode, HashSet<Triple>>(new FastVirtualNodeComparer(), MultiDictionaryMode.AVL);
            if (predIndex) _p = new MultiDictionary<INode, HashSet<Triple>>(new FastVirtualNodeComparer(), MultiDictionaryMode.AVL);
            if (objIndex) _o = new MultiDictionary<INode, HashSet<Triple>>(new FastVirtualNodeComparer(), MultiDictionaryMode.AVL);
            if (subjPredIndex) _sp = new MultiDictionary<Triple, HashSet<Triple>>(t => Tools.CombineHashCodes(t.Subject, t.Predicate), false, new SubjectPredicateComparer(new FastVirtualNodeComparer()), compoundIndexMode);
            if (subjObjIndex) _so = new MultiDictionary<Triple, HashSet<Triple>>(t => Tools.CombineHashCodes(t.Subject, t.Object), false, new SubjectObjectComparer(new FastVirtualNodeComparer()), compoundIndexMode);
            if (predObjIndex) _po = new MultiDictionary<Triple, HashSet<Triple>>(t => Tools.CombineHashCodes(t.Predicate, t.Object), false, new PredicateObjectComparer(new FastVirtualNodeComparer()), compoundIndexMode);
        }

        /// <summary>
        /// Indexes a Triple.
        /// </summary>
        /// <param name="t">Triple.</param>
        protected override void Index(Triple t)
        {
            IndexSimple(t.Subject, t, _s);
            IndexSimple(t.Predicate, t, _p);
            IndexSimple(t.Object, t, _o);
            IndexCompound(t, _sp);
            IndexCompound(t, _so);
            IndexCompound(t, _po);
        }

        /// <summary>
        /// Helper for indexing triples.
        /// </summary>
        /// <param name="n">Node to index by.</param>
        /// <param name="t">Triple.</param>
        /// <param name="index">Index to insert into.</param>
        private void IndexSimple(INode n, Triple t, MultiDictionary<INode, HashSet<Triple>> index)
        {
            if (index == null) return;

            if (index.TryGetValue(n, out HashSet<Triple> ts))
            {
                if (ts == null)
                {
                    index[n] = new HashSet<Triple> { t };
                }
                else
                {
                    ts.Add(t);
                }
            }
            else
            {
                index.Add(n, new HashSet<Triple> { t });
            }
        }

        /// <summary>
        /// Helper for indexing triples.
        /// </summary>
        /// <param name="t">Triple to index by.</param>
        /// <param name="index">Index to insert into.</param>
        private void IndexCompound(Triple t, MultiDictionary<Triple, HashSet<Triple>> index)
        {
            if (index == null) return;

            if (index.TryGetValue(t, out HashSet<Triple> ts))
            {
                if (ts == null)
                {
                    index[t] = new HashSet<Triple> { t };
                }
                else
                {
                    ts.Add(t);
                }
            }
            else
            {
                index.Add(t, new HashSet<Triple> { t });
            }
        }

        /// <summary>
        /// Unindexes a triple.
        /// </summary>
        /// <param name="t">Triple.</param>
        protected override void Unindex(Triple t)
        {
            UnindexSimple(t.Subject, t, _s);
            UnindexSimple(t.Predicate, t, _p);
            UnindexSimple(t.Object, t, _o);
            UnindexCompound(t, _sp);
            UnindexCompound(t, _so);
            UnindexCompound(t, _po);
        }

        /// <summary>
        /// Helper for unindexing triples.
        /// </summary>
        /// <param name="n">Node to index by.</param>
        /// <param name="t">Triple.</param>
        /// <param name="index">Index to remove from.</param>
        private void UnindexSimple(INode n, Triple t, MultiDictionary<INode, HashSet<Triple>> index)
        {
            if (index == null) return;

            if (index.TryGetValue(n, out HashSet<Triple> ts))
            {
                if (ts != null)
                {
                    ts.Remove(t);
                    if (ts.Count == 0)
                    {
                        index.Remove(new KeyValuePair<INode, HashSet<Triple>>(n, ts));
                    }
                }
            }
        }

        /// <summary>
        /// Helper for unindexing triples.
        /// </summary>
        /// <param name="t">Triple.</param>
        /// <param name="index">Index to remove from.</param>
        private void UnindexCompound(Triple t, MultiDictionary<Triple, HashSet<Triple>> index)
        {
            if (index == null) return;

            if (index.TryGetValue(t, out HashSet<Triple> ts))
            {
                if (ts != null)
                {
                    ts.Remove(t);
                    if (ts.Count == 0)
                    {
                        index.Remove(new KeyValuePair<Triple, HashSet<Triple>>(t, ts));
                    }
                }
            }
        }


        /// <summary>
        /// Gets the specific instance of a Triple in the collection.
        /// </summary>
        /// <param name="t">Triple.</param>
        /// <returns></returns>
        public override Triple this[Triple t]
        {
            get
            {
                if (Triples.TryGetKey(t, out Triple actual))
                {
                    return actual;
                }
                else
                {
                    throw new KeyNotFoundException("Given triple does not exist in this collection");
                }
            }
        }

        /// <summary>
        /// Gets all the triples with a given object.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        public override IEnumerable<Triple> WithObject(INode obj)
        {
            if (_o != null)
            {
                if (_o.TryGetValue(obj, out HashSet<Triple> ts))
                {
                    return ts ?? Enumerable.Empty<Triple>();
                }
                else
                {
                    return Enumerable.Empty<Triple>();
                }
            }
            else
            {
                return Triples.Keys.Where(t => t.Object.Equals(obj));
            }
        }

        /// <summary>
        /// Gets all the triples with a given predicate.
        /// </summary>
        /// <param name="pred">Predicate.</param>
        /// <returns></returns>
        public override IEnumerable<Triple> WithPredicate(INode pred)
        {
            if (_p != null)
            {
                if (_p.TryGetValue(pred, out HashSet<Triple> ts))
                {
                    return ts ?? Enumerable.Empty<Triple>();
                }
                else
                {
                    return Enumerable.Empty<Triple>();
                }
            }
            else
            {
                return Triples.Keys.Where(t => t.Predicate.Equals(pred));
            }
        }

        /// <summary>
        /// Gets all the triples with a given subject.
        /// </summary>
        /// <param name="subj">Subject.</param>
        /// <returns></returns>
        public override IEnumerable<Triple> WithSubject(INode subj)
        {
            if (_s != null)
            {
                if (_s.TryGetValue(subj, out HashSet<Triple> ts))
                {
                    return ts ?? Enumerable.Empty<Triple>();
                }
                else
                {
                    return Enumerable.Empty<Triple>();
                }
            }
            else
            {
                return Triples.Keys.Where(t => t.Subject.Equals(subj));
            }
        }

        /// <summary>
        /// Gets all the triples with a given predicate and object.
        /// </summary>
        /// <param name="pred">Predicate.</param>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        public override IEnumerable<Triple> WithPredicateObject(INode pred, INode obj)
        {
            if (_po != null)
            {
                if (_po.TryGetValue(new Triple(_subjVar, pred, obj), out HashSet<Triple> ts))
                {
                    return ts ?? Enumerable.Empty<Triple>();
                }
                else
                {
                    return Enumerable.Empty<Triple>();
                }
            }
            else
            {
                return WithPredicate(pred).Where(t => t.Object.Equals(obj));
            }
        }

        /// <summary>
        /// Gets all the triples with a given subject and object.
        /// </summary>
        /// <param name="subj">Subject.</param>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        public override IEnumerable<Triple> WithSubjectObject(INode subj, INode obj)
        {
            if (_so != null)
            {
                if (_so.TryGetValue(new Triple(subj, _predVar, obj), out HashSet<Triple> ts))
                {
                    return ts ?? Enumerable.Empty<Triple>();
                }
                else
                {
                    return Enumerable.Empty<Triple>();
                }
            }
            else
            {
                return WithSubject(subj).Where(t => t.Object.Equals(obj));
            }
        }

        /// <summary>
        /// Gets all the triples with a given subject and predicate.
        /// </summary>
        /// <param name="subj">Subject.</param>
        /// <param name="pred">Predicate.</param>
        /// <returns></returns>
        public override IEnumerable<Triple> WithSubjectPredicate(INode subj, INode pred)
        {
            if (_sp != null)
            {
                if (_sp.TryGetValue(new Triple(subj, pred, _objVar), out HashSet<Triple> ts))
                {
                    return ts ?? Enumerable.Empty<Triple>();
                }
                else
                {
                    return Enumerable.Empty<Triple>();
                }
            }
            else
            {
                return WithSubject(subj).Where(t => t.Predicate.Equals(pred));
            }
        }

        /// <summary>
        /// Gets the Object Nodes.
        /// </summary>
        public override IEnumerable<INode> ObjectNodes
        {
            get
            {
                return _o?.Keys ?? Triples.Keys.Select(t => t.Object);
            }
        }

        /// <summary>
        /// Gets the Predicate Nodes.
        /// </summary>
        public override IEnumerable<INode> PredicateNodes
        {
            get
            {
                return _p?.Keys ?? Triples.Keys.Select(t => t.Predicate);
            }
        }

        /// <summary>
        /// Gets the Subject Nodes.
        /// </summary>
        public override IEnumerable<INode> SubjectNodes
        {
            get
            {
                return _s?.Keys ?? Triples.Keys.Select(t => t.Subject);
            }
        }

        /// <summary>
        /// Disposes of the collection.
        /// </summary>
        public override void Dispose()
        {
            Triples.Clear();
            _s?.Clear();
            _p?.Clear();
            _o?.Clear();
            _so?.Clear();
            _sp?.Clear();
            _po?.Clear();
        }

        

    }
}
