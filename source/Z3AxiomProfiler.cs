﻿//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Z3AxiomProfiler.QuantifierModel;
using System.Linq;

namespace Z3AxiomProfiler
{
    public partial class Z3AxiomProfiler : Form
    {
        readonly bool launchedFromAddin;
        readonly Control ctrl;
        public string SearchText = "";
        SearchTree searchTree;
        readonly Dictionary<TreeNode, Common> expanded = new Dictionary<TreeNode, Common>();

        public Z3AxiomProfiler()
          : this(false, null)
        {
        }

        public Z3AxiomProfiler(bool launchedFromAddin, Control ctrl)
        {
            this.launchedFromAddin = launchedFromAddin;
            this.ctrl = ctrl;
            InitializeComponent();
        }

        private ParameterConfiguration parameterConfiguration = null;
        private Model model;

        private void Z3AxiomProfiler_OnLoadEvent(object sender, EventArgs e)
        {
            if (!this.launchedFromAddin)
            {
                if (parameterConfiguration != null)
                {
                    Loader.LoaderTask task = Loader.LoaderTask.LoaderTaskBoogie;

                    if (!string.IsNullOrEmpty(parameterConfiguration.z3LogFile))
                    {
                        task = Loader.LoaderTask.LoaderTaskParse;
                    }
                    loadModel(parameterConfiguration, task);
                    ParameterConfiguration.saveParameterConfigurationToSettings(parameterConfiguration);
                }
            }
        }

        private void LoadBoogie_Click(object sender, EventArgs e)
        {
            loadModelFromBoogie();
        }

        private void LoadZ3_Click(object sender, EventArgs e)
        {
            loadModelFromZ3();
        }

        private void LoadZ3Logfile_Click(object sender, EventArgs e)
        {
            loadModelFromZ3Logfile();
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        static string stripCygdrive(string s)
        {
            if (s.Length > 12 && s.StartsWith("/cygdrive/"))
            {
                s = s.Substring(10);
                return s.Substring(0, 1) + ":" + s.Substring(1);
            }
            else
                return s;
        }

        public bool parseCommandLineArguments(string[] args, out string error)
        {
            bool retval = false;
            int idx;

            ParameterConfiguration config = new ParameterConfiguration();

            config.boogieOptions = "/bv:z /trace";
            config.z3Options = "/rs:0";
            error = "";

            for (idx = 0; idx < args.Length; idx++)
            {
                args[idx] = stripCygdrive(args[idx]);
                if (args[idx].StartsWith("-")) args[idx] = "/" + args[idx].Substring(1);
                if (args[idx].StartsWith("/") && !System.IO.File.Exists(args[idx]))
                {
                    // parse command line parameter switches
                    if (args[idx].StartsWith("/f:"))
                    {
                        config.functionName = args[idx].Substring(3);
                    }
                    else if (args[idx].StartsWith("/l:"))
                    {
                        config.z3LogFile = args[idx].Substring(3);
                        // minimum requirements have been fulfilled.
                        retval = true;
                    }
                    else if (args[idx].StartsWith("/t:"))
                    {
                        uint timeout;
                        if (!UInt32.TryParse(args[idx].Substring(3), out timeout))
                        {
                            error = String.Format("Cannot parse timeout duration \"{0}\"", args[idx].Substring(3));
                            return false;
                        }
                        config.timeout = (int)timeout;
                    }
                    else if (args[idx].StartsWith("/c:"))
                    {
                        uint ch;
                        if (!UInt32.TryParse(args[idx].Substring(3), out ch))
                        {
                            error = String.Format("Cannot parse check number \"{0}\"", args[idx].Substring(3));
                            return false;
                        }
                        config.checkToConsider = (int)ch;
                    }
                    else if (args[idx] == "/v2")
                    {
                        // Silently accept old command line argument
                    }
                    else if (args[idx] == "/v1")
                    {
                        error = String.Format("Z3 version 1 is no longer supported.");
                        return false;
                    }
                    else if (args[idx] == "/s")
                    {
                        config.skipDecisions = true;
                    }
                    else
                    {
                        error = String.Format("Unknown command line argument \"{0}\".", args[idx]);
                        return false;
                    }
                }
                else
                {
                    bool isLogFile = false;
                    try
                    {
                        using (var s = File.OpenText(args[idx]))
                        {
                            var l = s.ReadLine();
                            if (l.StartsWith("[mk-app]") || l.StartsWith("Z3 error model") || l.StartsWith("partitions:") || l.StartsWith("*** MODEL"))
                                isLogFile = true;
                        }
                    }
                    catch (Exception)
                    {
                    }

                    if (isLogFile)
                    {
                        config.z3LogFile = args[idx];
                        retval = true;
                    }
                    else if (config.preludeBplFileInfo == null)
                    {
                        config.preludeBplFileInfo = new FileInfo(args[idx]);
                    }
                    else if (config.codeBplFileInfo == null)
                    {
                        config.codeBplFileInfo = new FileInfo(args[idx]);
                        // minimum requirements have been fulfilled.
                        retval = true;
                    }
                    else
                    {
                        error = "Multiple inputs files specified.";
                        return false;
                    }
                }
            }

            if (retval)
            {
                parameterConfiguration = config;
            }
            return true;
        }

        private string getZ3Setting(int randomSeed, bool randomSeedEnabled)
        {
            string defaultSetting = Properties.Settings.Default.Z3Options;
            string z3Options = String.Empty;
            string newRandomSeedArgument = String.Empty;

            if (randomSeedEnabled)
                newRandomSeedArgument = string.Format("/rs:{0}", randomSeed);

            //Is /rs in default Settings, then replace it.
            if (defaultSetting.Contains("/rs:"))
            {
                string oldArgument = String.Empty;

                string[] Arguments = defaultSetting.Split(' ');
                foreach (string Argument in Arguments)
                {
                    if (Argument.StartsWith("/rs:"))
                    {
                        oldArgument = Argument;
                    }
                }
                return defaultSetting.Replace(oldArgument, newRandomSeedArgument).Trim();
            }
            else //Is not, add it!
                return (defaultSetting + " " + newRandomSeedArgument).Trim();
        }

        //New Entry, witch is called from the AddIn
        public void load(string BPLFileName, string FunctionName, string PreludeFileName,
                         int randomSeed, bool randomSeedEnabled, string vcccmdswitches)
        {
            parameterConfiguration = new ParameterConfiguration();
            parameterConfiguration.functionName = FunctionName;
            parameterConfiguration.preludeBplFileInfo = new FileInfo(PreludeFileName);
            parameterConfiguration.codeBplFileInfo = new FileInfo(BPLFileName);
            parameterConfiguration.z3Options = getZ3Setting(randomSeed, randomSeedEnabled);

            loadModelFromBoogie();
        }

        public void loadModelFromBoogie()
        {
            LoadBoogieForm loadform = new LoadBoogieForm(launchedFromAddin, ctrl);
            if (parameterConfiguration != null)
            {
                loadform.setParameterConfiguration(parameterConfiguration);
            }
            else
            {
                loadform.reloadParameterConfiguration();
            }

            DialogResult dialogResult;

            dialogResult = loadform.ShowDialog();
            if (dialogResult != DialogResult.OK)
                return;

            parameterConfiguration = loadform.GetParameterConfiguration();
            ParameterConfiguration.saveParameterConfigurationToSettings(parameterConfiguration);

            loadModel(parameterConfiguration, Loader.LoaderTask.LoaderTaskBoogie);
        }

        public void loadModelFromZ3()
        {
            LoadZ3Form loadform = new LoadZ3Form(launchedFromAddin, ctrl);
            if (parameterConfiguration != null)
            {
                loadform.setParameterConfiguration(parameterConfiguration);
            }
            else
            {
                loadform.reloadParameterConfiguration();
            }

            DialogResult dialogResult;

            dialogResult = loadform.ShowDialog();
            if (dialogResult != DialogResult.OK)
                return;

            parameterConfiguration = loadform.GetParameterConfiguration();
            ParameterConfiguration.saveParameterConfigurationToSettings(parameterConfiguration);

            loadModel(parameterConfiguration, Loader.LoaderTask.LoaderTaskZ3);
        }

        public void loadModelFromZ3Logfile()
        {
            LoadZ3LogForm loadform = new LoadZ3LogForm(launchedFromAddin, ctrl);
            if (parameterConfiguration != null)
            {
                loadform.setParameterConfiguration(parameterConfiguration);
            }
            else
            {
                loadform.reloadParameterConfiguration();
            }

            DialogResult dialogResult;

            dialogResult = loadform.ShowDialog();
            if (dialogResult != DialogResult.OK)
                return;

            parameterConfiguration = loadform.GetParameterConfiguration();
            ParameterConfiguration.saveParameterConfigurationToSettings(parameterConfiguration);

            loadModel(parameterConfiguration, Loader.LoaderTask.LoaderTaskParse);
        }

        private void loadModel(ParameterConfiguration config, Loader.LoaderTask task)
        {
            // Create a new loader and LoadingProgressForm and execute the loading
            Loader loader = new Loader(config, task);
            LoadingProgressForm lprogf = new LoadingProgressForm(loader);
            lprogf.ShowDialog();

            model = loader.GetModel();
            loadTree();
        }

        private void loadTree()
        {
            this.Text = model.LogFileName + ": Z3 Axiom Profiler";
            z3AxiomTree.Nodes.Clear();

            if (model.conflicts.Count > 0)
            {
                AddTopNode(Common.Callback("CONFLICTS", () => model.conflicts));
                AddTopNode(Common.Callback("100 CONFLICTS", () => RandomConflicts(100)));
            }

            if (model.proofSteps.ContainsKey(0))
            {
                AddTopNode(model.proofSteps[0]);
                AddTopNode(model.SetupImportantInstantiations());
            }

            model.NewModel();
            foreach (var c in model.models)
                AddTopNode(c);

            model.PopScopes(model.scopes.Count - 1, null, 0);

            var rootSD = model.scopes[0];
            Scope root = rootSD.Scope;
            if (rootSD.Literal == null)
            {
                foreach (var i in rootSD.Implied)
                    System.Diagnostics.Debug.Assert(i.Id == Model.MarkerLiteral.Id);
            }
            else
            {
                var l = new Literal();
                l.Id = -1;
                l.Term = new Term("root", new Term[0]);
                rootSD.Implied.Insert(0, rootSD.Literal);
                l.Implied = rootSD.Implied.ToArray();
                root.Literals.Add(l);
            }
            root.PropagateImpliedByChildren();
            root.ComputeConflictCost(new List<Conflict>());
            root.AccountLastDecision(model);
            model.rootScope = root;

            var fInfo = parameterConfiguration.preludeBplFileInfo;
            GraphVizualization.DumpGraph(model, fInfo?.FullName ?? "<unknown>");

            List<Quantifier> quantByCnfls = model.quantifiers.Values.Where(q => q.GeneratedConflicts > 0).ToList();
            quantByCnfls.Sort((q1, q2) => q2.GeneratedConflicts.CompareTo(q1.GeneratedConflicts));
            if (quantByCnfls.Count > 0)
                AddTopNode(Common.Callback("Quant. by last conflict", () => quantByCnfls));

            AddTopNode(root);

            foreach (Quantifier q in model.GetQuantifiersSortedByInstantiations())
            {
                AddTopNode(q);
            }
            z3AxiomTree.ShowNodeToolTips = true;
        }


        private int AddTopNode(Common cfl)
        {
            return z3AxiomTree.Nodes.Add(makeNode(cfl));
        }



        void HandleExpand(object sender, TreeViewCancelEventArgs args)
        {
            if (args == null) return;

            TreeNode node = args.Node;
            if (expanded.ContainsKey(node))
                return;
            expanded[node] = null;

            if (!(node.Tag is Common))
                return;

            z3AxiomTree.BeginUpdate();
            Common nodeTag = (Common)node.Tag;
            node.Nodes.Clear();
            foreach (var c in nodeTag.Children())
                node.Nodes.Add(makeNode(c));

            node.EnsureVisible();
            z3AxiomTree.EndUpdate();
        }

        private string ToolTipProcessor(string tip)
        {
            if (tip.Length <= 80) return tip;

            // This is too long. Try to truncate the string...
            // Assume that tool tips with multiple lines are OK.
            // So just truncate each line around position 80.
            string[] lines = tip.Replace("\r", "").Split('\n');
            string tt = "";
            foreach (string line in lines)
            {
                if (line.Length < 80)
                {
                    tt += line + "\n";
                }
                else
                {
                    tt += line.Substring(0, 80) + "...\n";
                }
            }

            return tt;
        }

        public TreeNode ExpandScope(Scope s)
        {
            var coll = z3AxiomTree.Nodes;
            if (s.parentScope != null)
            {
                var p = ExpandScope(s.parentScope);
                if (p == null) return null;
                coll = p.Nodes;
            }

            foreach (TreeNode n in coll)
            {
                if (n.Tag == s)
                {
                    n.Expand();
                    z3AxiomTree.SelectedNode = n;
                    return n;
                }
            }

            return null;
        }

        internal TreeNode makeNode(Common common)
        {
            var label = common.ToString();
            var n = 100;
            if (label.Length > n)
                label = label.Substring(0, n) + "...";
            TreeNode cNode = new TreeNode(label);
            cNode.Tag = common;
            cNode.ToolTipText = ToolTipProcessor(common.ToolTip());
            if (common.ForeColor() != 0)
                cNode.ForeColor = Color.FromArgb(common.ForeColor());
            if (common.HasChildren())
                cNode.Nodes.Add(new TreeNode("dummy node"));
            if (common.AutoExpand())
                cNode.Expand();
            return cNode;
        }

        private void colorVisualizationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (model == null)
                return;
            ColorVisalizationForm colorForm = new ColorVisalizationForm(launchedFromAddin, ctrl);
            colorForm.quantifierLinkedText.Click += nodeSelector;
            colorForm.setQuantifiers(model.GetQuantifiersSortedByOccurence(), model.GetQuantifiersSortedByInstantiations());
            colorForm.Show();
        }

        private void nodeSelector(object sender, EventArgs args)
        {
            if (sender is LinkLabel)
            {
                LinkLabel ll = (LinkLabel)sender;
                foreach (TreeNode node in z3AxiomTree.Nodes)
                {
                    if ((node != null) && (node.Text != null))
                    {

                        if (ll.Text == node.Text)
                        {
                            z3AxiomTree.SelectedNode = node;
                        }
                    }
                }
            }
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            HelpWindow h = new HelpWindow();
            h.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().Show();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            loadModelFromBoogie();
        }

        private void conflictsAsCSVToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void allConflictsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (model.conflicts == null) return;
            ConflictsToCsv(model.conflicts);
        }

        private List<Conflict> RandomConflicts(int n)
        {
            List<Conflict> res = new List<Conflict>();

            if (model.conflicts == null) return res;

            double sum = 0;
            foreach (var c in model.conflicts)
                sum += c.InstCost;

            int id = 0;
            Random r = new Random(0);
            while (id++ < n)
            {
                int line = r.Next((int)sum);
                double s = 0;
                foreach (var c in model.conflicts)
                {
                    s += c.InstCost;
                    if (s > line)
                    {
                        res.Add(c);
                        break;
                    }
                }
            }

            return res;
        }

        private void randomConflictsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (model.conflicts == null) return;
            ConflictsToCsv(RandomConflicts(1000));
        }

        private static void ConflictsToCsv(List<Conflict> cc)
        {
            StringBuilder sb = Conflict.CsvHeader();
            int id = 0;
            foreach (var c in cc)
            {
                c.PrintAsCsv(sb, ++id);
            }
            Clipboard.SetText(sb.ToString());
        }

        private void HandleTreeNodeClick(object sender, TreeViewEventArgs e)
        {
            TreeNode t = z3AxiomTree.SelectedNode;
            Common c = t.Tag as Common;
            SetToolTip(c);

            Scope scope = c as Scope;
            if (scope != null)
            {
                searchTree?.SelectScope(scope);
            }

            Instantiation inst = c as Instantiation;
            if (inst != null)
            {
                SetInstantiationPath(inst);
            }
        }

        private void SetToolTip(Common c)
        {
            if (c != null)
            {
                toolTipBox.Lines = c.ToolTip().Replace("\r", "").Split('\n');
            }
        }

        private void SetInstantiationPath(Instantiation inst)
        {
            // delete old content
            InstantiationPathView.BeginUpdate();
            InstantiationPathView.Items.Clear();

            List<Instantiation> instantiationPath = model.LongestPathWithInstantiation(inst);
            foreach (Instantiation i in instantiationPath)
            {
                ListViewItem item = new ListViewItem
                {
                    Text = i.Depth.ToString(),
                    Name = $"Quantifier Instantiation {i.FingerPrint}"
                };
                item.Tag = i;
                item.SubItems.Add(i.FingerPrint);
                item.SubItems.Add(i.Quant.Qid);
                item.SubItems.Add(i.Quant.Instances.Count.ToString());

                InstantiationPathView.Items.Add(item);

                if (i != inst) continue;

                item.BackColor = Color.GreenYellow;
                item.Focused = true;
                item.EnsureVisible();
            }
            InstantiationPathView.EndUpdate();
        }

        void Search()
        {
            var searchBox = new SearchBox(this);
            searchBox.Populate(z3AxiomTree.Nodes);
            searchBox.Show();
        }

        internal void Activate(TreeNode n)
        {
            z3AxiomTree.SelectedNode = n;
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Search();
        }

        private void z3AxiomTree_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '/':
                    e.Handled = true;
                    Search();
                    break;
                case (char)27:
                    e.Handled = true;
                    this.Close();
                    break;
                case 'v':
                    e.Handled = true;
                    ShowTree();
                    break;
                case '\r':
                    if (z3AxiomTree.SelectedNode != null)
                        z3AxiomTree.SelectedNode.Expand();
                    e.Handled = true;
                    break;
            }
        }

        private void Z3AxiomProfiler_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void searchTreeVisualizationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowTree();
        }

        private void ShowTree()
        {
            if (searchTree == null)
                searchTree = new SearchTree(model, this);
            searchTree.Show();
        }

        private void PathItemClick(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Common c = e.Item.Tag as Common;
            if (c != null)
            {
                SetToolTip(c);
            }
        }

        private void z3AxiomTree_Enter(object sender, EventArgs e)
        {
            TreeNode t = z3AxiomTree.SelectedNode;
            if (t == null) return;
            Common c = t.Tag as Common;
            SetToolTip(c);
        }

        private void InstantiationPathView_Enter(object sender, EventArgs e)
        {
            if (InstantiationPathView.SelectedItems.Count <= 0) return;
            Common c = InstantiationPathView.SelectedItems[0].Tag as Common;
            SetToolTip(c);
        }
    }
}
