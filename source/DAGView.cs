﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Z3AxiomProfiler.QuantifierModel;
using Color = Microsoft.Msagl.Drawing.Color;
using MouseButtons = System.Windows.Forms.MouseButtons;
using Size = System.Drawing.Size;

namespace Z3AxiomProfiler
{
    public partial class DAGView : Form
    {

        private readonly Z3AxiomProfiler _z3AxiomProfiler;
        private readonly GViewer _viewer;
        private Graph graph;
        private static int newNodeWarningThreshold = 20;

        //Define the colors
        private readonly List<Color> colors = new List<Color> {Color.Purple, Color.Blue,
                Color.Green, Color.LawnGreen, Color.Orange, Color.Cyan, Color.DarkGray, Color.Moccasin,
                Color.YellowGreen, Color.Silver, Color.Salmon, Color.LemonChiffon, Color.Fuchsia,
                Color.ForestGreen, Color.Beige
                };

        private static readonly Color selectionColor = Color.Red;
        private static readonly Color parentColor = Color.Yellow;

        private readonly Dictionary<Quantifier, Color> colorMap = new Dictionary<Quantifier, Color>();
        private int currColorIdx;

        public DAGView(Z3AxiomProfiler profiler)
        {
            _z3AxiomProfiler = profiler;
            InitializeComponent();
            //create a viewer object 
            _viewer = new GViewer
            {
                AsyncLayout = true,
                EdgeInsertButtonVisible = false,
                LayoutEditingEnabled = false,
                //LayoutAlgorithmSettingsButtonVisible = false,
                NavigationVisible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Top = panel1.Bottom,
                Left = Left,
                Size = new Size(Right, Bottom - panel1.Bottom)
            };
            _viewer.MouseMove += _ViewerMouseMove;
            _viewer.MouseUp += _ViewerViewMouseUp;
            _viewer.MouseClick += _ViewerViewClick;
            //associate the viewer with the form 
            Controls.Add(_viewer);
        }

        private void drawGraph()
        {
            Text = $"Instantiations dependencies [{maxRenderDepth.Value} levels]";

            var edgeRoutingSettings = new EdgeRoutingSettings
            {
                EdgeRoutingMode = EdgeRoutingMode.Spline,
                BendPenalty = 50
            };
            var layoutSettings = new SugiyamaLayoutSettings
            {
                AspectRatio = 4,
                LayerSeparation = 10,
                EdgeRoutingSettings = edgeRoutingSettings
            };

            //create a graph object
            graph = new Graph($"Instantiations dependencies [{maxRenderDepth.Value} levels]")
            {
                LayoutAlgorithmSettings = layoutSettings
            };

            foreach (var node in _z3AxiomProfiler.model.instances
                .Where(inst => inst.Depth <= maxRenderDepth.Value)
                .Select(connectToVisibleNodes))
            {
                formatNode(node);
            }

            //bind the graph to the viewer 
            _viewer.Graph = graph;
        }

        private void formatNode(Node currNode)
        {
            var inst = _z3AxiomProfiler.model.fingerprints[currNode.Id];
            currNode.UserData = inst;
            var nodeColor = getColor(inst.Quant);
            currNode.Attr.FillColor = nodeColor;
            if (nodeColor.R * 0.299 + nodeColor.G * 0.587 + nodeColor.B * 0.114 <= 186)
            {
                currNode.Label.FontColor = Color.White;
            }
            currNode.LabelText = inst.SummaryInfo();
        }

        private void maxRenderDepth_ValueChanged(object sender, EventArgs e)
        {
            drawGraph();
        }

        private Color getColor(Quantifier quant)
        {
            if (!colorMap.ContainsKey(quant) && currColorIdx >= colors.Count)
            {
                return Color.Black;
            }
            if (colorMap.ContainsKey(quant)) return colorMap[quant];

            colorMap[quant] = colors[currColorIdx];
            currColorIdx++;
            return colorMap[quant];
        }

        private void DAGView_Load(object sender, EventArgs e)
        {
            drawGraph();
        }


        private int oldX = -1;
        private int oldY = -1;
        private void _ViewerMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (oldX != -1 && oldY != -1)
            {
                _viewer.Pan(e.X - oldX, e.Y - oldY);
            }
            oldX = e.X;
            oldY = e.Y;
        }

        private void _ViewerViewMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            oldX = -1;
            oldY = -1;
        }

        private Node previouslySelectedNode;
        private void _ViewerViewClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var node = _viewer.SelectedObject as Node;
            selectNode(node);
        }

        private void selectNode(Node node)
        {
            if (previouslySelectedNode != null)
            {
                unselectNode();
            }


            if (node != null)
            {
                // format new one
                node.Attr.FillColor = selectionColor;
                node.Label.FontColor = Color.White;
                // plus all parents
                foreach (var inEdge in node.InEdges)
                {
                    inEdge.SourceNode.Attr.FillColor = parentColor;
                    inEdge.SourceNode.Label.FontColor = Color.Black;
                }
                previouslySelectedNode = node;
                _z3AxiomProfiler.SetToolTip((Instantiation)node.UserData);
            }
            _viewer.Invalidate();
        }

        private void unselectNode()
        {
            // restore old node
            formatNode(previouslySelectedNode);

            // plus all parents
            foreach (var inEdge in previouslySelectedNode.InEdges)
            {
                formatNode(inEdge.SourceNode);
            }
            previouslySelectedNode = null;
        }

        private void hideInstantiationButton_Click(object sender, EventArgs e)
        {
            if (previouslySelectedNode == null)
            {
                return;
            }
            var nodeToRemove = previouslySelectedNode;
            unselectNode();

            // delete subgraph dependent on only the node being deleted
            Queue<Node> todoRemoveNodes = new Queue<Node>();
            todoRemoveNodes.Enqueue(nodeToRemove);
            while (todoRemoveNodes.Count > 0)
            {
                var currNode = todoRemoveNodes.Dequeue();
                foreach (var edge in currNode.OutEdges.Where(edge => edge.TargetNode.InEdges.Count() == 1))
                {
                    todoRemoveNodes.Enqueue(edge.TargetNode);
                }
                graph.RemoveNode(currNode);
            }

            _viewer.Graph = graph;
            redrawGraph();
        }

        private void showParentsButton_Click(object sender, EventArgs e)
        {
            if (previouslySelectedNode == null)
            {
                return;
            }

            Instantiation inst = (Instantiation)previouslySelectedNode.UserData;
            foreach (var parentInst in inst.ResponsibleInstantiations
                .Where(parentInst => graph.FindNode(parentInst.FingerPrint) == null))
            {
                connectToVisibleNodes(parentInst);
                formatNode(graph.FindNode(parentInst.FingerPrint));
            }

            redrawGraph();
        }

        private void showChildrenButton_Click(object sender, EventArgs e)
        {
            if (previouslySelectedNode == null)
            {
                return;
            }
            var currInst = (Instantiation)previouslySelectedNode.UserData;
            var newNodeInsts = currInst.DependantInstantiations
                                       .Where(childInst => graph.FindNode(childInst.FingerPrint) == null)
                                       .ToList();
            if (checkNumNodesWithDialog(currInst, ref newNodeInsts)) return;

            foreach (var childInst in newNodeInsts)
            {
                connectToVisibleNodes(childInst);
                formatNode(graph.FindNode(childInst.FingerPrint));
            }
            redrawGraph();
        }

        private bool checkNumNodesWithDialog(Instantiation currInst, ref List<Instantiation> newNodeInsts)
        {
            if (newNodeInsts.Count > newNodeWarningThreshold)
            {
                var filterDecision = MessageBox.Show(
                    $"This operation would add {newNodeInsts.Count} new nodes to the graph. It is recommended to reduce the number by filtering.\nWould you like to filter the new nodes now?",
                    $"{newNodeInsts.Count} new nodes warning",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);
                switch (filterDecision)
                {
                    case DialogResult.Yes:
                        var filterBox = new InstantiationFilter(currInst.DependantInstantiations
                            .Where(childInst => graph.FindNode(childInst.FingerPrint) == null)
                            .ToList());
                        if (filterBox.ShowDialog() == DialogResult.Cancel)
                        {
                            // just stop
                            return true;
                        }
                        newNodeInsts = filterBox.filtered;
                        break;
                    case DialogResult.Cancel:
                        // just stop
                        return true;
                }
            }
            return false;
        }

        private void redrawGraph()
        {
            _viewer.NeedToCalculateLayout = true;
            _viewer.Graph = graph;
            selectNode(previouslySelectedNode);
        }

        private void showChainButton_Click(object sender, EventArgs e)
        {
            if (previouslySelectedNode == null)
            {
                return;
            }
            // other direction
            var current = (Instantiation)previouslySelectedNode.UserData;
            while (current.DependantInstantiations.Count > 0)
            {
                var parent = current;
                // follow the longest path
                current = current.DependantInstantiations
                    .Aggregate((i1, i2) => i1.DeepestSubpathDepth > i2.DeepestSubpathDepth ? i1 : i2);

                // add edge, implicitly add node
                graph.AddEdge(parent.FingerPrint, current.FingerPrint);

                // format node
                var node = graph.FindNode(current.FingerPrint);
                formatNode(node);
                connectToVisibleNodes(current);
            }

            _viewer.NeedToCalculateLayout = true;
            _viewer.Graph = graph;
        }

        private Node connectToVisibleNodes(Instantiation instantiation)
        {
            var instNode = graph.FindNode(instantiation.FingerPrint) ?? graph.AddNode(instantiation.FingerPrint);
            var fingerprint = instantiation.FingerPrint;

            // add edges for the instantiation's visible parents
            // if the edge is not already there
            foreach (var parentInst in instantiation.ResponsibleInstantiations
                .Where(inst => graph.FindNode(inst.FingerPrint) != null)
                .Where(parentInst => instNode.InEdges.All(edge => edge.Source != parentInst.FingerPrint)))
            {
                graph.AddEdge(parentInst.FingerPrint, fingerprint);
            }

            // add in-edges for the instantiation's visible children
            // if the edge is not already there
            foreach (var child in instantiation.DependantInstantiations
                .Where(inst => graph.FindNode(inst.FingerPrint) != null)
                .Where(child => instNode.OutEdges.All(edge => edge.Target != child.FingerPrint)))
            {
                graph.AddEdge(fingerprint, child.FingerPrint);
            }
            return instNode;
        }
    }
}
