using System;
using System.Collections.Generic;
using Microsoft.Msagl.Drawing;
using AxiomProfiler.QuantifierModel;

namespace SubstructureTest
{
    // A visulization and explination for the test graphs can be found here
    // https://docs.google.com/document/d/1SJspfBecgkVT9U8xC-MvQ_NPDTGVfg0yqq7YjJ3ma2s/edit?usp=sharing
    // Some graphs 
    class TestGraphs2
    {
        public List<Quantifier> Quants = new List<Quantifier>(); // Quantifiers used in unit tests
        public BindingInfo Info; // BindingInfo used to make nodes
        public Graph graph1;
        public List<Node> path1, path2;

        // Constructor
        // Mkae the graphs needsed for testing
        public TestGraphs2()
        {
            InitQuantsAndInfo();
            MakePath1();
            MakePath2();
        }

        // initialize 9 quantifiers
        private void InitQuantsAndInfo()
        {
            // initialize Quants
            for (int i = 0; i < 10; i++)
            {
                Quants.Add(new Quantifier());
                Quants[i].PrintName = i.ToString();
            }
            // initialize Info, with meaningless arguments
            Term[] args = new Term[0];
            Term Term = new Term("a", args);
            Info = new BindingInfo(Quants[0], args, args);
        }

        // function that make of node with Id = nodeId,
        // and UserDate = a instantiation with quantifier quant
        private Node MakeNode(String nodeId, int quant)
        {
            Node node = new Node(nodeId);
            Instantiation Inst = new Instantiation(Info, "a");
            Inst.Quant = Quants[quant];
            node.UserData = Inst;
            return node;
        }

        // unlike in TestGraph.cs, we will use numbers for node id intead of
        // only Uppercase latin letters.
        private void MakePath1()
        {
            path1 = new List<Node>() { MakeNode("0", 0), MakeNode("1", 1), MakeNode("2", 2), 
                MakeNode("3", 0), MakeNode("4", 1), MakeNode("5", 2) };
        }

        private void MakePath2()
        {
            path2 = new List<Node>() { MakeNode("0", 0), MakeNode("1", 1), MakeNode("2", 2),
                MakeNode("3", 0), MakeNode("4", 1), MakeNode("5", 2), MakeNode("6", 0),
                MakeNode("7", 1) };
        }
    }
}