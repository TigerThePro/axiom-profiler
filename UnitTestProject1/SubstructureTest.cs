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
        static List<Quantifier> pattern = new List<Quantifier>() { Graphs.Quants[1], Graphs.Quants[2], Graphs.Quants[0] };

        [TestMethod]
        public void RefindPatternTest1()
        {
            List<Quantifier> result = DAGView.RefindPattern(ref Graphs.path1, ref pattern);
            List<Quantifier> expected = new List<Quantifier>() { Graphs.Quants[0], Graphs.Quants[1], Graphs.Quants[2] };
            Assert.AreEqual(expected.Count, result.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], result[i]);
            }
        }

        [TestMethod]
        public void RefindPatternTest2()
        {
            List<Quantifier> result = DAGView.RefindPattern(ref Graphs.path2, ref pattern);
            List<Quantifier> expected = new List<Quantifier>() { Graphs.Quants[2], Graphs.Quants[0], Graphs.Quants[1] };
            Assert.AreEqual(expected.Count, result.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], result[i]);
            }
        }
    }

    // Given a cycle find the 'abstract strucutre' of this cycle that should occur at other full cycles
    // The strucurre is represent as a spanning tree "rooted" on the last node of the cycle
    [TestClass]
    public class FindSubStructureTest
    {
        static TestGraphs2 Graphs = new TestGraphs2();
        
        [TestMethod]
        public void FindSubStructureTest1()
        {
            //List<List<Quantifier>> = DAGView.FindSubStrucutre(Graphs.graph1.FindNode("3, "))
            Graph g = new Graph();
            Node n = new Node("1");
            n.UserData = "123";
            g.AddNode(n);
            Console.WriteLine(g.FindNode("1").UserData);
            g.FindNode("1").UserData = "456";
            Console.WriteLine(g.FindNode("1").UserData);
            Assert.Equals("456", g.FindNode("1").UserData);
        }
    }

    // Using the path, and newly found spanning tree,
    // select a subgrph around the path that stasfying the spanning tree
    [TestClass]
    public class FindSubGraphTest
    {

    }
}