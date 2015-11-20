﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Z3AxiomProfiler.QuantifierModel;

namespace Z3AxiomProfiler
{
    public partial class InstantiationFilter : Form
    {
        // original, unfiltered list of instantiations
        private readonly List<Instantiation> original;
        public List<Instantiation> filtered; 
        public InstantiationFilter(List<Instantiation> instantiationsToFilter)
        {
            InitializeComponent();
            original = instantiationsToFilter;
        }

        protected override void OnLoad(EventArgs e)
        {
            var quantifiers = original.Select(inst => inst.Quant).Distinct().ToList();
            foreach (var quant in quantifiers)
            {
                quantSelectionBox.Items.Add(quant, true);
            }
            doFilter();
        }

        private void doFilter()
        {
            filtered = original
                .Where(inst => inst.Depth <= maxDepthUpDown.Value)
                .Where(inst => quantSelectionBox.CheckedItems.Contains(inst.Quant))
                .OrderByDescending(inst => inst.Cost)
                .Take((int) maxNewNodesUpDown.Value)
                .ToList();
            numberNodesMatchingLabel.Text = filtered.Count.ToString();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void quantSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            BeginInvoke((Action)doFilter);
        }

        private void updateFilter(object sender, EventArgs e)
        {
            doFilter();
        }
    }
}