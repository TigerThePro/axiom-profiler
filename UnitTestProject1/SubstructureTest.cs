using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using AxiomProfiler;
using AxiomProfiler.QuantifierModel;
using Microsoft.Msagl.Drawing;


// Unit tests for a feature that find repeating substrucutre in a graph.
// A path needs to already found by 'explain path'
// details are found in the link below
// https://docs.google.com/document/d/1KxL2duPp-eAQk3P9TdL9vpipSBtMzrFYqsXMsIzQhnY/edit?usp=sharing
namespace SubstructureTest
{
    // "Refind" the pattern, so that the path ends with a full cycle of the pattern
    // e.g. for 'B->C->A->B->C' the redefined pattern would be 'A->B->C'
    [TestClass]
    public class RefindPatternTest
    {
        static TestGraphs2 Graphs = new TestGraphs2();
        [TestMethod]
        public void RefindPatternTest1()
        {
            //Assert.AreEqual(1, 2);
        }
    }

    // Given a cycle find the 'abstract strucutre' of this cycle that should occur at other full cycles
    // The strucurre is represent as a spanning tree "rooted" on the last node of the cycle
    [TestClass]
    public class MakeSpanningTreeTest
    {

    }

    // Using the path, and newly found spanning tree,
    // select a subgrph around the path that stasfying the spanning tree
    [TestClass]
    public class FindSubStructureTest
    {

    }
}