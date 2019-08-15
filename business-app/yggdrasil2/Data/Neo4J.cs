using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using shift.yggdrasil2.Topology;

using Neo4j.Driver.V1;

namespace shift.yggdrasil2.data
{
    public class Neo4J
    {
        public string Neo4J_URI { get; set; }
        public string Neo4J_User { get; set; }
        public string Neo4J_Password { get; set; }

        private IStatementResult GetIGPTopology()
        {
            Config config = new Config();
            config.EncryptionLevel = EncryptionLevel.None;

            using (var driver = GraphDatabase.Driver(Neo4J_URI, AuthTokens.Basic(Neo4J_User, Neo4J_Password), config))
            using (var session = driver.Session())
            {
                StringBuilder query = new StringBuilder();

                query.AppendLine(@"MATCH p=(a:Node)-[l:Link]-(b) WHERE a.topology_id='igp' AND exists(l.IPv4_Interface_Address) AND (b:Node OR b:Pseudonode) RETURN p");

                return session.Run(query.ToString());
            }
        }

        private IStatementResult GetMPLSTopology()
        {
            Config config = new Config();
            config.EncryptionLevel = EncryptionLevel.None;

            using (var driver = GraphDatabase.Driver(Neo4J_URI, AuthTokens.Basic(Neo4J_User, Neo4J_Password), config))
            using (var session = driver.Session())
            {
                StringBuilder query = new StringBuilder();

                query.AppendLine(@"MATCH p=(a:Node)-[l:LSP]-(b:Node) WHERE a.topology_id='mpls' and b.topology_id='mpls' RETURN p");

                return session.Run(query.ToString());
            }
        }

        public void LoadIGPTopology(Topology.IGP.Topology topology)
        {
            topology.Clear();

            foreach (var record in GetIGPTopology())
            {
                var path = record.Values["p"].As<IPath>();

                Dictionary<long, string> nodeDatabaseIds = new Dictionary<long, string>();
                Dictionary<long, bool> pseudonodes = new Dictionary<long, bool>();

                foreach (var node in path.Nodes)
                {
                    string igpRouterIdentifier = TryGetNodePropertyAsString(node, "IGP_Router_Identifier");

                    if (!string.IsNullOrWhiteSpace(igpRouterIdentifier))
                    {
                        nodeDatabaseIds.Add(node.Id, igpRouterIdentifier);

                        Topology.Node.Node n = new Topology.Node.Node()
                        {
                            IgpRouterIdentifier = igpRouterIdentifier,
                            Psn = TryGetNodeProperty<int>(node, "psn"),
                            IPv4RouterIdentifier = TryGetNodePropertyAsString(node, "IPv4_Router_Identifier"),
                            NodeName = TryGetNodePropertyAsString(node, "Node_Name"),
                            FirstSeen = TryGetNodeProperty<long>(node, "First_Seen"),
                            LastEvent = TryGetNodeProperty<long>(node, "Last_Event"),
                            OperationalStatus = true
                        };

                        if (Regex.IsMatch(igpRouterIdentifier, @"^\d{12}$") && n.Psn == 0)
                        {
                            n.IsPseudonode = false;
                            n.NodeCost = 1;

                            pseudonodes.Add(node.Id, n.IsPseudonode);
                        }
                        else
                        {
                            if (Regex.IsMatch(igpRouterIdentifier, @"^\d{12}\d+$") && n.Psn != 0)
                            {
                                n.IsPseudonode = true;
                                n.NodeCost = 0;

                                pseudonodes.Add(node.Id, n.IsPseudonode);
                            }
                        }

                        topology.Update(n);
                    }
                }

                foreach (var link in path.Relationships)
                {
                    Topology.IGP.Link.Link l = new Topology.IGP.Link.Link()
                    {
                        SourceNode = nodeDatabaseIds[link.StartNodeId],
                        TargetNode = nodeDatabaseIds[link.EndNodeId],
                        Asn = TryGetRelProperty<long>(link, "asn"),
                        MaximumLinkBandwidth =  TryGetRelProperty<double>(link,"Maximum_Link_Bandwidth"),
                        MaximumReservableLinkBandwidth = TryGetRelProperty<double>(link, "Maximum_Reservable_Bandwidth"),
                        UnreservedBandwidth = TryGetRelPropertyAsArray<double>(link, "Unreserved_Bandwidth"),
                        Rtt = TryGetRelProperty<long>(link, "rtt"),
                        IPv4InterfaceAddress = TryGetRelPropertyAsString(link, "IPv4_Interface_Address"),
                        SharedRiskLinkGroups = TryGetRelPropertyAsArray<long>(link, "Shared_Risk_Link_Groups"),
                        FirstSeen = TryGetRelProperty<long>(link, "First_Seen"),
                        LastEvent = TryGetRelProperty<long>(link, "Last_Event"),
                        OperationalStatus = true
                    };

                    if (pseudonodes[link.StartNodeId]  || pseudonodes[link.EndNodeId])
                    {
                        l.LinkCost = 0.5;
                    }
                    else
                    {
                        l.LinkCost = 1;
                    }

                    topology.Update(l);
                }
            }
        }

        public Topology.MPLS.Topology LoadMPLSTopology()
        {
            Topology.MPLS.Topology topology = new Topology.MPLS.Topology();

            foreach (var record in GetMPLSTopology())
            {
                var path = record.Values["p"].As<IPath>();

                Dictionary<long, string> nodeIPv4RouterIdentifiersByDatabaseId = new Dictionary<long, string>();
                Dictionary<long, string> nodePCCByDatabaseId = new Dictionary<long, string>();

                foreach (var node in path.Nodes)
                {
                    string ipv4RouterIdentifier = TryGetNodePropertyAsString(node, "IPv4_Router_Identifier");
                    string pcc = TryGetNodePropertyAsString(node, "PCC");

                    if (!string.IsNullOrWhiteSpace(ipv4RouterIdentifier))
                    {
                        nodeIPv4RouterIdentifiersByDatabaseId.Add(node.Id, ipv4RouterIdentifier);
                        nodePCCByDatabaseId.Add(node.Id, pcc);

                        Topology.Node.Node n = new Topology.Node.Node()
                        {
                            IPv4RouterIdentifier = TryGetNodePropertyAsString(node, "IPv4_Router_Identifier"),
                            PCC = pcc,
                            //NodeName = TryGetNodePropertyAsString(node, "Node_Name"),
                            FirstSeen = TryGetNodeProperty<long>(node, "First_Seen"),
                            //LastEvent = TryGetNodeProperty<long>(node, "Last_Event"),
                            OperationalStatus = true
                        };

                        n.IsPseudonode = false;
                        n.NodeCost = 1;

                        topology.Update(n);
                    }
                }

                foreach (var lsp in path.Relationships)
                {

                    var symbolicPathName = TryGetRelPropertyAsString(lsp, "Symbolic_Path_Name");

                    if (symbolicPathName.Contains("/"))
                    {
                        var hierarchicalSymbolicPathName = symbolicPathName.Split("/".ToCharArray());

                        Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath parent = new Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath(hierarchicalSymbolicPathName[0])
                        {
                            IPv4TunnelSenderAddress = nodeIPv4RouterIdentifiersByDatabaseId[lsp.StartNodeId],
                            IPv4TunnelEndpointAddress = nodeIPv4RouterIdentifiersByDatabaseId[lsp.EndNodeId]
                        };

                        topology.Update(parent);

                        Topology.MPLS.LabelSwitchedPath.LabelSwitchedPath child = new Topology.MPLS.LabelSwitchedPath.LabelSwitchedPath(parent.Id)
                        {
                            PCC = nodePCCByDatabaseId[lsp.StartNodeId], // Gets the PCC parameter from the StartNode instead of the LSP property, SHIFT PCEP listener is behind Docker Swarm NAT(PAT).
                            IPv4TunnelSenderAddress = nodeIPv4RouterIdentifiersByDatabaseId[lsp.StartNodeId],
                            IPv4TunnelEndpointAddress = nodeIPv4RouterIdentifiersByDatabaseId[lsp.EndNodeId],
                            SymbolicPathName = symbolicPathName,
                            TunnelIdentifier = TryGetRelPropertyAsString(lsp, "Tunnel_Identifier"),
                            ExtendedTunnelIdentifier = TryGetRelPropertyAsString(lsp, "Extended_Tunnel_Identifier"),
                            ExtendedTunnelIdentifierTunnelId = TryGetRelPropertyAsString(lsp, "Extended_Tunnel_Identifier_tunnel_id"),
                            LspIdentifier = TryGetRelPropertyAsString(lsp, "LSP_Identifier"),
                            ReservedBandwidth = TryGetRelProperty<long>(lsp, "Reserved_Bandwidth"),
                            Administrative = TryGetRelProperty<bool>(lsp, "Administrative"),
                            Delegate = TryGetRelProperty<bool>(lsp, "Delegate"),
                            Operational = TryGetRelProperty<bool>(lsp, "Operational"),
                            Remove = TryGetRelProperty<bool>(lsp, "Remove"),
                            Sync = TryGetRelProperty<bool>(lsp, "Sync"),
                            RecordRouteObject = TryGetRelPropertyAsArray<string>(lsp, "Record_Route_Object"),
                            FirstSeen = TryGetRelProperty<long>(lsp, "First_Seen"),
                            LastEvent = TryGetRelProperty<long>(lsp, "Last_Event")
                        };

                        topology.Update(child);
                    }
                    else
                    {
                        Topology.MPLS.LabelSwitchedPath.LabelSwitchedPath l = new Topology.MPLS.LabelSwitchedPath.LabelSwitchedPath(null)
                        {
                            PCC = TryGetRelPropertyAsString(lsp, "PCC"),
                            IPv4TunnelSenderAddress = nodeIPv4RouterIdentifiersByDatabaseId[lsp.StartNodeId],
                            IPv4TunnelEndpointAddress = nodeIPv4RouterIdentifiersByDatabaseId[lsp.EndNodeId],
                            SymbolicPathName = symbolicPathName,
                            TunnelIdentifier = TryGetRelPropertyAsString(lsp, "Tunnel_Identifier"),
                            ExtendedTunnelIdentifier = TryGetRelPropertyAsString(lsp, "Extended_Tunnel_Identifier"),
                            ExtendedTunnelIdentifierTunnelId = TryGetRelPropertyAsString(lsp, "Extended_Tunnel_Identifier_tunnel_id"),
                            LspIdentifier = TryGetRelPropertyAsString(lsp, "LSP_Identifier"),
                            ReservedBandwidth = TryGetRelProperty<long>(lsp, "Reserved_Bandwidth"),
                            Administrative = TryGetRelProperty<bool>(lsp, "Administrative"),
                            Delegate = TryGetRelProperty<bool>(lsp, "Delegate"),
                            Operational = TryGetRelProperty<bool>(lsp, "Operational"),
                            Remove = TryGetRelProperty<bool>(lsp, "Remove"),
                            Sync = TryGetRelProperty<bool>(lsp, "Sync"),
                            RecordRouteObject = TryGetRelPropertyAsArray<string>(lsp, "Record_Route_Object"),
                            FirstSeen = TryGetRelProperty<long>(lsp, "First_Seen"),
                            LastEvent = TryGetRelProperty<long>(lsp, "Last_Event")
                        };

                        topology.Update(l);
                    }
                }
            }

            return topology;
        }

        public static T TryGetNodeProperty<T>(INode Node, String PropertyName) where T : new()
        {
            try
            {
                return Node.Properties[PropertyName].As<T>();
            }
            catch (Exception)
            {
                return new T();
            }
        }

        public static string TryGetNodePropertyAsString(INode Node, String PropertyName)
        {
            try
            {
                return Node.Properties[PropertyName].As<string>();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static T TryGetRelProperty<T>(IRelationship Rel, String PropertyName) where T : new()
        {
            try
            {
                return Rel.Properties[PropertyName].As<T>();
            }
            catch (Exception)
            {
                return new T();
            }
        }

        public static T[] TryGetRelPropertyAsArray<T>(IRelationship Rel, String PropertyName)
        {
            try
            {
                return Rel.Properties[PropertyName].As<List<T>>().ToArray();
            }
            catch (Exception)
            {
                return new T[0];
            }
        }

        public static string TryGetRelPropertyAsString(IRelationship Rel, String PropertyName)
        {
            try
            {
                return Rel.Properties[PropertyName].As<string>();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
