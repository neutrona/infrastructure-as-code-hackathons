using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using shift.yggdrasil2.Topology.MPLS;
using shift.yggdrasil2.Topology.IGP;

namespace shift.ui.architect.code
{
    class OutputProcessor
    {
        public static void IntentProcessor(object result, yggdrasil2.Topology.MPLS.Topology mpls_topology,
            yggdrasil2.Topology.IGP.Topology igp_topology, Dataweb.NShape.WinFormsUI.Display display,
            TreeView treeViewResult, ToolStripProgressBar toolStripProgressBar)
        {
            toolStripProgressBar.Visible = true;

            Application.DoEvents();

            if (result != null)
            {
                if (result.GetType() == typeof(yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath))
                {
                    yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath hlsp =
                        (yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath)result;

                    Application.DoEvents();

                    // Validate Paths
                    yggdrasil2.PathComputation.PathValidation.ValidatePathsUsingDijkstra(igp_topology, hlsp);

                    Application.DoEvents();

                    // TreeView Result
                    treeViewResult.Nodes.Clear();

                    Application.DoEvents();

                    FillTreeView(hlsp, treeViewResult, display, igp_topology);
                }
                else
                {
                    if (result.GetType() == typeof(List<yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath>))
                    {
                        List<yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath> hlsps =
                        (List<yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath>)result;

                        Application.DoEvents();

                        //Validate Paths
                        yggdrasil2.PathComputation.PathValidation.ValidatePathsUsingDijkstra(igp_topology, hlsps);

                        Application.DoEvents();

                        // TreeView Result
                        treeViewResult.Nodes.Clear();

                        Application.DoEvents();

                        FillTreeView(hlsps, treeViewResult, display, igp_topology);
                    }
                } 
            }

            toolStripProgressBar.Visible = false;
        }

        public static void FillTreeView(List<yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath> hlsps,
            TreeView treeViewResult, Dataweb.NShape.WinFormsUI.Display display, yggdrasil2.Topology.IGP.Topology igp_topology)
        {
            foreach (var hlsp in hlsps)
            {
                FillTreeView(hlsp, treeViewResult, display, igp_topology);
            }
        }

        public static void FillTreeView(yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath hlsp,
            TreeView treeViewResult, Dataweb.NShape.WinFormsUI.Display display, yggdrasil2.Topology.IGP.Topology igp_topology)
        {

            TreeNode hlspNode = new TreeNode(hlsp.SymbolicPathName);
            hlspNode.Tag = hlsp;

            treeViewResult.Nodes.Add(hlspNode);

            foreach (var lsp in hlsp.Children)
            {
                TreeNode lspNode = new TreeNode(lsp.SymbolicPathName);
                lspNode.Tag = lsp;

                if (!lsp.Feasible)
                {
                    lspNode.ForeColor = System.Drawing.Color.Red;
                }

                TreeNode computedERONode = new TreeNode("Computed ERO");

                computedERONode.Tag = lsp;

                lspNode.Nodes.Add(computedERONode);

                foreach (var hop in lsp.ComputedExplicitRouteObject)
                {
                    TreeNode hopNode = new TreeNode(hop);

                    var link = igp_topology.Links.Where(l => l.IPv4InterfaceAddress == hop).SingleOrDefault();

                    if (link != null)
                    {
                        hopNode.Tag = new ui.display.UI.ShapeTag(link.Id, link);
                    }

                    computedERONode.Nodes.Add(hopNode);
                }

                hlspNode.Nodes.Add(lspNode);

                lspNode.Expand();
            }

            hlspNode.Expand();
        }
    }
}
