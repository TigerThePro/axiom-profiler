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
            MakeGraph1();
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

        private void MakeGraph1()
        {
            graph1 = new Graph();

            graph1.AddNode(MakeNode("0", 0));
            graph1.AddNode(MakeNode("1", 1));
            graph1.AddNode(MakeNode("2", 0));
            graph1.AddNode(MakeNode("3", 1));
            graph1.AddNode(MakeNode("4", 0));
            graph1.AddNode(MakeNode("5", 1));
            graph1.AddNode(MakeNode("6", 0));
            graph1.AddNode(MakeNode("7", 1));

            graph1.AddNode(MakeNode("8", 2));
            graph1.AddNode(MakeNode("9", 3));
            graph1.AddNode(MakeNode("10", 2));
            graph1.AddNode(MakeNode("11", 3));
            graph1.AddNode(MakeNode("12", 2));
            graph1.AddNode(MakeNode("13", 3));
            graph1.AddNode(MakeNode("14", 2));
            graph1.AddNode(MakeNode("15", 3));

            graph1.AddNode(MakeNode("16", 4));
            graph1.AddNode(MakeNode("17", 4));
            graph1.AddNode(MakeNode("18", 4));
            graph1.AddNode(MakeNode("19", 4));

            for (int i = 0; i < 7; i++)
            {
                graph1.AddEdge(i.ToString(), (i + 1).ToString());
            }
            for (int i = 0; i <= 4; i += 2)
            {
                graph1.AddEdge(i.ToString(), (i + 7).ToString());
                graph1.AddEdge((i + 7).ToString(), (i + 8).ToString());
                graph1.AddEdge((i + 8).ToString(), (i + 2).ToString());
            }
            graph1.AddEdge("6", "14");
            graph1.AddEdge("14", "15");
            graph1.AddEdge("16", "1");
            graph1.AddEdge("17", "3");
            graph1.AddEdge("18", "5");
            graph1.AddEdge("19", "7");
        }
    }
}