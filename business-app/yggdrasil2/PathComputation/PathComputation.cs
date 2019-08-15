using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

#region QuickGraph
using QuickGraph;
using QuickGraph.Algorithms.ShortestPath;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.RankedShortestPath;
using QuickGraph.Collections;
using QuickGraph.Algorithms;
#endregion

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace shift.yggdrasil2.PathComputation
{
    public class PathComputation
    {
        public class YggdrasilNM2 : IDisposable
        {

            // Flag: Has Dispose already been called?
            bool disposed = false;
            // Instantiate a SafeHandle instance.
            SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);


            public Dictionary<string, Topology.Node.Node> Nodes { get; set; }
            public Dictionary<string, Topology.IGP.Link.Link> Links { get; set; }

            public BidirectionalGraph<string, TaggedEdge<string, Topology.IGP.Link.Link>> Graph { get; set; }

            public Dictionary<TaggedEdge<string, Topology.IGP.Link.Link>, double> EdgeCost { get; set; }

            public HoffmanPavleyRankedShortestPathAlgorithm<string, TaggedEdge<string, Topology.IGP.Link.Link>> HoffmanPavley { get; set; }

            public void LoadGraph(Topology.IGP.Topology Topology)
            {
                List<Topology.Node.Node> nodes_copy = Topology.Nodes.DeepClone();
                List<Topology.IGP.Link.Link> links_copy = Topology.Links.DeepClone();

                foreach (var n in nodes_copy)
                {
                    this.AddVertex(n);
                }
                foreach (var l in links_copy)
                {
                    this.AddEdge(l);
                }
            }

            public void LoadGraph(Topology.IGP.Topology Topology, string EdgeCostPropertyName)
            {
                List<Topology.Node.Node> nodes_copy = Topology.Nodes.DeepClone();
                List<Topology.IGP.Link.Link> links_copy = Topology.Links.DeepClone();

                foreach (var n in nodes_copy)
                {
                    this.AddVertex(n);
                }
                foreach (var l in links_copy)
                {
                    this.AddEdge(l, EdgeCostPropertyName);
                }
            }

            private void AddVertex(Topology.Node.Node Vertex)
            {
                if (!this.Nodes.ContainsKey(Vertex.Id))
                {
                    this.Nodes.Add(Vertex.Id, Vertex);
                    this.Graph.AddVertex(Vertex.Id);
                }

            }

            private void AddEdge(Topology.IGP.Link.Link Edge)
            {
                if (!this.Links.ContainsKey(Edge.SourceNode))
                {
                    this.Links.Add(Edge.Id, Edge);

                    if (Edge.OperationalStatus)
                    {
                        var forwardEdge = new TaggedEdge<string, Topology.IGP.Link.Link>(Edge.SourceNode, Edge.TargetNode, Edge);
                        var backwardEdge = new TaggedEdge<string, Topology.IGP.Link.Link>(Edge.TargetNode, Edge.SourceNode, Edge);

                        this.Graph.AddEdge(forwardEdge);
                        this.Graph.AddEdge(backwardEdge);

                        this.EdgeCost.Add(forwardEdge, Convert.ToDouble(Edge.Rtt));
                        this.EdgeCost.Add(backwardEdge, Convert.ToDouble(Edge.Rtt));
                    }
                }
            }

            private void AddEdge(Topology.IGP.Link.Link Edge, string EdgeCostPropertyName)
            {
                if (!this.Links.ContainsKey(Edge.SourceNode))
                {
                    this.Links.Add(Edge.Id, Edge);

                    if (Edge.OperationalStatus)
                    {
                        var forwardEdge = new TaggedEdge<string, Topology.IGP.Link.Link>(Edge.SourceNode, Edge.TargetNode, Edge);
                        var backwardEdge = new TaggedEdge<string, Topology.IGP.Link.Link>(Edge.TargetNode, Edge.SourceNode, Edge);

                        this.Graph.AddEdge(forwardEdge);
                        this.Graph.AddEdge(backwardEdge);

                        this.EdgeCost.Add(forwardEdge, Convert.ToDouble(Edge.GetType().GetProperty(EdgeCostPropertyName).GetValue(Edge)));
                        this.EdgeCost.Add(backwardEdge, Convert.ToDouble(Edge.GetType().GetProperty(EdgeCostPropertyName).GetValue(Edge)));
                    }
                }
            }

            public void ComputeModel()
            {
                foreach (var startNode in this.Nodes.Where(n => !n.Value.IsPseudonode))
                {
                    foreach (var endNode in this.Nodes.Where(n => !n.Value.IsPseudonode))
                    {
                        if (startNode.Key != endNode.Key)
                        {
                            Tunnel tunnel = new Tunnel();

                            this.HoffmanPavley.Compute(startNode.Key, endNode.Key);

                            List<Path> computedPaths = new List<Path>();

                            foreach (var path in this.HoffmanPavley.ComputedShortestPaths)
                            {
                                computedPaths.Add(new Path()
                                {
                                    Hops = path.Select(h => h.Tag).ToList()
                                });
                            }
                        }
                    }
                }
            }

            public Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath ComputeModel(Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath HierarchicalLabelSwitchedPath)
            {
                var source_node = this.Nodes.Where(n => n.Value.IPv4RouterIdentifier == HierarchicalLabelSwitchedPath.IPv4TunnelSenderAddress).SingleOrDefault();
                var target_node = this.Nodes.Where(n => n.Value.IPv4RouterIdentifier == HierarchicalLabelSwitchedPath.IPv4TunnelEndpointAddress).SingleOrDefault();


                if (source_node.Value != null && target_node.Value != null)
                {
                    Tunnel tunnel = new Tunnel();

                    this.HoffmanPavley.Compute(source_node.Key, target_node.Key);

                    List<Path> computedPaths = new List<Path>();

                    foreach (var path in this.HoffmanPavley.ComputedShortestPaths)
                    {
                        computedPaths.Add(new Path()
                        {
                            Hops = path.Select(h => h.Tag).ToList()
                        });
                    }

                    #region Return Optimisation

                    if (HierarchicalLabelSwitchedPath.Children.Count <= computedPaths.Count)
                    {
                        for (int i = 0; i < HierarchicalLabelSwitchedPath.Children.Count; i++)
                        {
                            // Select only the actual next hops, for point to point and point to multipoint IGP links
                            List<Topology.IGP.Link.Link> actual_next_hops = new List<Topology.IGP.Link.Link>();

                            for (int j = 0; j < computedPaths[i].Hops.Count; j++)
                            {
                                var current_hop = computedPaths[i].Hops[j];
                                var current_hop_target_node = this.Nodes[current_hop.TargetNode];

                                if (current_hop_target_node.IsPseudonode)
                                {
                                    int k = j + 1;
                                    if (k < computedPaths[i].Hops.Count)
                                    {
                                        var next_hop = computedPaths[i].Hops[k];
                                        actual_next_hops.Add(next_hop);
                                        j++;
                                    }
                                }
                                else
                                {
                                    actual_next_hops.Add(current_hop);
                                }
                            }

                            string[] computedHops = actual_next_hops.Select(h => h.IPv4InterfaceAddress).ToArray();

                            HierarchicalLabelSwitchedPath.Children[i].ComputedExplicitRouteObject = computedHops;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < computedPaths.Count; i++)
                        {
                            // Select only the actual next hops, for point to point and point to multipoint IGP links
                            List<Topology.IGP.Link.Link> actual_next_hops = new List<Topology.IGP.Link.Link>();

                            for (int j = 0; j < computedPaths[i].Hops.Count; j++)
                            {
                                var current_hop = computedPaths[i].Hops[j];
                                var current_hop_target_node = this.Nodes[current_hop.TargetNode];

                                if (current_hop_target_node.IsPseudonode)
                                {
                                    int k = j + 1;
                                    if (k < computedPaths[i].Hops.Count)
                                    {
                                        var next_hop = computedPaths[i].Hops[k];
                                        actual_next_hops.Add(next_hop);
                                        j++;
                                    }
                                }
                                else
                                {
                                    actual_next_hops.Add(current_hop);
                                }
                            }

                            string[] computedHops = actual_next_hops.Select(h => h.IPv4InterfaceAddress).ToArray();

                            HierarchicalLabelSwitchedPath.Children[i].ComputedExplicitRouteObject = computedHops;
                        }
                    }
                    #endregion
                }

                return HierarchicalLabelSwitchedPath;
            }

            public YggdrasilNM2()
            {
                this.Nodes = new Dictionary<string, Topology.Node.Node>();
                this.Links = new Dictionary<string, Topology.IGP.Link.Link>();
                this.Graph = new BidirectionalGraph<string, TaggedEdge<string, Topology.IGP.Link.Link>>();
                this.EdgeCost = new Dictionary<TaggedEdge<string, Topology.IGP.Link.Link>, double>();
                this.HoffmanPavley = new HoffmanPavleyRankedShortestPathAlgorithm<string, TaggedEdge<string, Topology.IGP.Link.Link>>
                    (this.Graph, AlgorithmExtensions.GetIndexer<TaggedEdge<string, Topology.IGP.Link.Link>, double>(this.EdgeCost));
                this.HoffmanPavley.ShortestPathCount = 100;
            }

            // Public implementation of Dispose pattern callable by consumers.
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            // Protected implementation of Dispose pattern.
            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                    handle.Dispose();
                    // Free any other managed objects here.
                    //
                }

                disposed = true;
            }

        }
    }

    public static class ExtensionMethods
    {
        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
