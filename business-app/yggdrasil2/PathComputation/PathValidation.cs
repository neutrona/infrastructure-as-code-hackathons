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


namespace shift.yggdrasil2.PathComputation
{
    public class PathValidation
    {

        public static void ValidatePathsUsingDijkstra(Topology.IGP.Topology igp_topology, List<Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath> hlsps)
        {
            foreach (var hlsp in hlsps)
            {
                ValidatePathsUsingDijkstra(igp_topology, hlsp);
            }
        }

        public static void ValidatePathsUsingDijkstra(Topology.IGP.Topology igp_topology, Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath hlsp)
        {

            Console.WriteLine("\n====Validating Paths====\n");

            List<yggdrasil2.Topology.Node.Node> nodes_copy = igp_topology.Nodes.DeepClone();
            List<yggdrasil2.Topology.IGP.Link.Link> links_copy = igp_topology.Links.DeepClone();

            BidirectionalGraph<string, TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>> graph =
                new BidirectionalGraph<string, TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>>();

            Dictionary<TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>, double> cost =
                new Dictionary<TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>, double>();


            var start = nodes_copy.Where(n => n.IPv4RouterIdentifier == hlsp.IPv4TunnelSenderAddress).SingleOrDefault();
            var end = nodes_copy.Where(n => n.IPv4RouterIdentifier == hlsp.IPv4TunnelEndpointAddress).SingleOrDefault();

            if (start != null && end != null)
            {
                foreach (var lsp in hlsp.Children)
                {
                    graph.Clear();

                    foreach (var node in nodes_copy)
                    {
                        graph.AddVertex(node.Id);
                    }

                    if (!start.IsPseudonode)  // it will never be a pseudonode, get rid of this
                    {
                        var nodeLinks = links_copy.Where(l => l.SourceNode == start.Id).ToList();

                        foreach (var l in nodeLinks)
                        {
                            if (!graph.ContainsEdge(l.SourceNode, l.TargetNode))
                            {
                                if (l.OperationalStatus)
                                {
                                    var forwardEdge = new TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>(l.SourceNode, l.TargetNode, l);
                                    graph.AddEdge(forwardEdge);
                                    cost.Add(forwardEdge, 1);
                                }
                            }
                        }
                    }

                    foreach (var hop in lsp.ComputedExplicitRouteObject)
                    {
                        var link = links_copy.Where(l => l.IPv4InterfaceAddress == hop).SingleOrDefault();

                        if (link != null)
                        {
                            if (!graph.ContainsEdge(link.SourceNode, link.TargetNode))
                            {

                                if (link.OperationalStatus)
                                {
                                    var backwardEdge = new TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>(link.TargetNode, link.SourceNode, link);
                                    graph.AddEdge(backwardEdge);
                                    cost.Add(backwardEdge, 1);
                                }

                                //var srcNode = nodes_copy.Where(n => n.IgpRouterIdentifier == link.SourceNode).SingleOrDefault();
                                var dstNode = nodes_copy.Where(n => n.IgpRouterIdentifier == link.TargetNode).SingleOrDefault();

                                //if (srcNode != null)
                                //{

                                //    if (srcNode.IsPseudonode)
                                //    {
                                //        var nodeLinks = links_copy.Where(l => l.TargetNode == srcNode.Id).ToList();

                                //        foreach (var l in nodeLinks)
                                //        {

                                //            if (!graph.ContainsEdge(l.SourceNode, l.TargetNode))
                                //            {
                                //                if (l.OperationalStatus)
                                //                {
                                //                    var forwardEdge = new TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>(l.SourceNode, l.TargetNode, l);
                                //                    graph.AddEdge(forwardEdge);
                                //                    cost.Add(forwardEdge, 1);
                                //                }
                                //            }
                                //        }
                                //    }

                                //}

                                if (dstNode != null)
                                {

                                    if (dstNode.IsPseudonode)
                                    {
                                        var nodeLinks = links_copy.Where(l => l.TargetNode == dstNode.Id).ToList();

                                        foreach (var l in nodeLinks)
                                        {

                                            if (!graph.ContainsEdge(l.SourceNode, l.TargetNode))
                                            {
                                                if (l.OperationalStatus)
                                                {
                                                    var forwardEdge = new TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>(l.SourceNode, l.TargetNode, l);
                                                    graph.AddEdge(forwardEdge);
                                                    cost.Add(forwardEdge, 1);
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }

                    DijkstraShortestPathAlgorithm<string, TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>> dijkstra =
                        new DijkstraShortestPathAlgorithm<string, TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>>(graph,
                        AlgorithmExtensions.GetIndexer<TaggedEdge<string, yggdrasil2.Topology.IGP.Link.Link>, double>(cost));

                    dijkstra.Compute(start.Id);

                    if (dijkstra.Distances.ContainsKey(end.Id) && dijkstra.Distances[end.Id] != double.MaxValue)
                    {
                        lsp.Feasible = true;
                        Console.WriteLine("Path {0} is \u001b[32mFEASIBLE\u001b[0m.\n\t{1} is REACHABLE from {2} in {3} hops (includes pseudonodes).",
                            lsp.SymbolicPathName, lsp.IPv4TunnelEndpointAddress, lsp.IPv4TunnelSenderAddress, dijkstra.Distances[end.Id]);
                    }
                    else
                    {
                        lsp.Feasible = false;
                        Console.WriteLine("Path {0} is \u001b[31mNOT FEASIBLE\u001b[0m.\n\t{1} is UREACHABLE from {2}.",
                            lsp.SymbolicPathName, lsp.IPv4TunnelEndpointAddress, lsp.IPv4TunnelSenderAddress);
                    }
                }
            }
        }

    }
}
