using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using shift.yggdrasil2.ExtensionMethods;

#region JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endregion

namespace shift.yggdrasil2.Topology.MPLS
{
    public class Topology
    {
        public const string TopologyId = "mpls";
        public List<Node.Node> Nodes { get; }
        public List<LabelSwitchedPath.LabelSwitchedPath> LabelSwitchedPaths { get; }
        public List<HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath> HierarchicalLabelSwitchedPaths { get; }

        public bool locked { get; private set; }

        public Topology()
        {
            this.Nodes = new List<Node.Node>();
            this.LabelSwitchedPaths = new List<LabelSwitchedPath.LabelSwitchedPath>();
            this.HierarchicalLabelSwitchedPaths = new List<HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath>();
        }

        public async void SetBaseline()
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            foreach (var hlsp in this.HierarchicalLabelSwitchedPaths)
            {
                foreach (var lsp in hlsp.Children)
                {
                    lsp.ComputedExplicitRouteObjectBaseline = lsp.ComputedExplicitRouteObject.DeepClone();
                }
            }

            this.locked = false;
        }

        public async void Update(Node.Node Node)
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            Node.TopologyId = TopologyId;


            var existingNode = Nodes.Find(n => n.IPv4RouterIdentifier == Node.IPv4RouterIdentifier);

            if (existingNode == null && Node.OperationalStatus)
            {
                Nodes.Add(Node);
            }
            else
            {
                if (Node.OperationalStatus)
                {
                    Nodes[Nodes.IndexOf(existingNode)] = Node;
                }
                else
                {
                    Nodes.Remove(Node);
                }
            }

            this.locked = false;
        }

        public async void Update(LabelSwitchedPath.LabelSwitchedPath LabelSwitchedPath)
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            if (LabelSwitchedPath.IsChild)
            {
                /* Id is based on Tunnel Sender and Receiver addresses, this does not work with JunOS implementation of 
                * PCRept message that sends LSPs with Remove flag set and missing Tunnel Sender and Receiver addresses = "0.0.0.0"
                *
                var parent = this.HierarchicalLabelSwitchedPaths.Find(h => h.Id == LabelSwitchedPath.ParentId);
                */

                var parentName = LabelSwitchedPath.SymbolicPathName.Split("/".ToCharArray())[0];
                var parent = this.HierarchicalLabelSwitchedPaths.Find(h => h.SymbolicPathName == parentName);

                /* Id is based on Tunnel Sender and Receiver addresses, this does not work with JunOS implementation of 
                * PCRept message that sends LSPs with Remove flag set and missing Tunnel Sender and Receiver addresses = "0.0.0.0"
                *
                var existingLabelSwitchedPath = parent.Children.Find(e => e.Id == LabelSwitchedPath.Id);
                */
                var existingLabelSwitchedPath = parent.Children.Find(e => e.SymbolicPathName == LabelSwitchedPath.SymbolicPathName);
                if (existingLabelSwitchedPath == null)
                {
                    // Check if LSP needs to be removed
                    if (LabelSwitchedPath.Remove.HasValue && LabelSwitchedPath.Remove.Value)
                    {
                        // Do not add if remove is true. Topology is out of sync.
                    } else
                    {
                        parent.Children.Insert(0, LabelSwitchedPath);
                    }
                }
                else
                {
                    // Check if LSP needs to be removed
                    if (LabelSwitchedPath.Remove.HasValue && LabelSwitchedPath.Remove.Value)
                    {
                        parent.Children.RemoveAt(parent.Children.IndexOf(existingLabelSwitchedPath));
                    }
                    else
                    {
                        LabelSwitchedPath.ComputedExplicitRouteObjectBaseline = existingLabelSwitchedPath.ComputedExplicitRouteObjectBaseline.DeepClone(); // Move previously computed RRO baseline to updated LSP
                        LabelSwitchedPath.ComputedExplicitRouteObject = existingLabelSwitchedPath.ComputedExplicitRouteObject.DeepClone(); // Move previously computed RRO to updated LSP
                        parent.Children[parent.Children.IndexOf(existingLabelSwitchedPath)] = LabelSwitchedPath;
                    }
                }

                parent.Children = parent.Children.OrderBy(o => o.SymbolicPathName).ToList();

                if(parent.Children.Count == 0)
                {
                    this.HierarchicalLabelSwitchedPaths.RemoveAt(this.HierarchicalLabelSwitchedPaths.IndexOf(parent));
                }
            }
            else
            {
                var existingLabelSwitchedPath =
                LabelSwitchedPaths.Find(l => l.IPv4TunnelSenderAddress == LabelSwitchedPath.IPv4TunnelSenderAddress && 
                l.IPv4TunnelEndpointAddress == LabelSwitchedPath.IPv4TunnelEndpointAddress);
                if (existingLabelSwitchedPath == null)
                {
                    LabelSwitchedPaths.Add(LabelSwitchedPath);
                }
                else
                {
                    LabelSwitchedPath.ComputedExplicitRouteObjectBaseline = existingLabelSwitchedPath.ComputedExplicitRouteObjectBaseline.DeepClone(); // Move previously computed RRO baseline to updated LSP
                    LabelSwitchedPath.ComputedExplicitRouteObject = existingLabelSwitchedPath.ComputedExplicitRouteObject.DeepClone(); // Move previously computed RRO to updated LSP
                    LabelSwitchedPaths[LabelSwitchedPaths.IndexOf(existingLabelSwitchedPath)] = LabelSwitchedPath;
                }
            }

            this.locked = false;
        }

        public async void Update(HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath HierarchicalLabelSwitchedPath)
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            /* Id is based on Tunnel Sender and Receiver addresses, this does not work with JunOS implementation of 
             * PCRept message that sends LSPs with Remove flag set and missing Tunnel Sender and Receiver addresses = "0.0.0.0"
             *
            var existingHierarchicalLabelSwitchedPath =
                HierarchicalLabelSwitchedPaths.Find(l => l.Id == HierarchicalLabelSwitchedPath.Id);
            */

            var existingHierarchicalLabelSwitchedPath =
                HierarchicalLabelSwitchedPaths.Find(l => l.SymbolicPathName == HierarchicalLabelSwitchedPath.SymbolicPathName);

            if (existingHierarchicalLabelSwitchedPath == null)
            {
                HierarchicalLabelSwitchedPaths.Add(HierarchicalLabelSwitchedPath);
            }

            // UPDATE? (can overwrite children!)

            this.locked = false;
        }

        public void Update(JObject data)
        {
            switch ((string)data["yggdrasil-data-type"])
            {
                case "shift-pcep-message":
                    ProcessSHIFTPCEPMessage(data);
                    break;
                default:
                    // Unknown data type
                    break;
            }

        }

        private async void ProcessSHIFTPCEPMessage(JObject data)
        {
            var symbolicPathName = (string)data["lsp"]["symbolic_path_name"];

            if (!string.IsNullOrWhiteSpace(symbolicPathName))
            {
                var ipv4_tunnel_sender_address = (string)data["lsp"]["ipv4_tunnel_sender_address"];
                var ipv4_tunnel_endpoint_address = (string)data["lsp"]["ipv4_tunnel_endpoint_address"];

                // Gets the PCC parameter from the local node database instead of the LSP property, SHIFT PCEP listener is behind Docker Swarm's NAT(PAT).
                var ingress_node = this.Nodes.Find(n => n.IPv4RouterIdentifier == ipv4_tunnel_sender_address);
                string pcc = string.Empty;
                if (ingress_node != null)
                {
                    pcc = ingress_node.PCC;
                }

                if (symbolicPathName.Contains("/"))
                {
                    var hierarchicalSymbolicPathName = symbolicPathName.Split("/".ToCharArray());

                    HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath parent = new HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath(hierarchicalSymbolicPathName[0])
                    {
                        IPv4TunnelSenderAddress = ipv4_tunnel_sender_address,
                        IPv4TunnelEndpointAddress = ipv4_tunnel_endpoint_address
                    };

                    this.Update(parent);

                    LabelSwitchedPath.LabelSwitchedPath child = new LabelSwitchedPath.LabelSwitchedPath(parent.Id)
                    {
                        PCC = pcc,
                        IPv4TunnelSenderAddress = ipv4_tunnel_sender_address,
                        IPv4TunnelEndpointAddress = ipv4_tunnel_endpoint_address,
                        SymbolicPathName = symbolicPathName,
                        TunnelIdentifier = (string)data["lsp"]["tunnel_id"],
                        ExtendedTunnelIdentifier = (string)data["lsp"]["extended_tunnel_id"],
                        ExtendedTunnelIdentifierTunnelId = (string)data["lsp"]["extended_tunnel_id"],
                        LspIdentifier = (string)data["lsp"]["lsp_id"],
                        ReservedBandwidth = (long?)data["bandwidth"],
                        Administrative = (bool)data["lsp"]["flags"]["administrative"],
                        Delegate = (bool)data["lsp"]["flags"]["delegate"],
                        Operational = (bool)data["lsp"]["flags"]["operational"],
                        Remove = (bool)data["lsp"]["flags"]["remove"],
                        Sync = (bool)data["lsp"]["flags"]["sync"]
                    };

                    if (data["rro"] != null)
                    {
                        List<string> hops = new List<string>();

                        foreach (var hop in data["rro"])
                        {
                            hops.Add((string)hop["address"]);
                        }

                        child.RecordRouteObject = hops.ToArray();
                    }

                    this.Update(child);
                }
                else
                {
                    LabelSwitchedPath.LabelSwitchedPath l = new LabelSwitchedPath.LabelSwitchedPath(null)
                    {
                        PCC = pcc,
                        IPv4TunnelSenderAddress = ipv4_tunnel_sender_address,
                        IPv4TunnelEndpointAddress = ipv4_tunnel_endpoint_address,
                        SymbolicPathName = symbolicPathName,
                        TunnelIdentifier = (string)data["lsp"]["tunnel_id"],
                        ExtendedTunnelIdentifier = (string)data["lsp"]["extended_tunnel_id"],
                        ExtendedTunnelIdentifierTunnelId = (string)data["lsp"]["extended_tunnel_id"],
                        LspIdentifier = (string)data["lsp"]["lsp_id"],
                        ReservedBandwidth = (long?)data["bandwidth"],
                        Administrative = (bool)data["lsp"]["flags"]["administrative"],
                        Delegate = (bool)data["lsp"]["flags"]["delegate"],
                        Operational = (bool)data["lsp"]["flags"]["operational"],
                        Remove = (bool)data["lsp"]["flags"]["remove"],
                        Sync = (bool)data["lsp"]["flags"]["sync"]
                    };

                    if (data["lsp"]["rro"] != null)
                    {
                        List<string> hops = new List<string>();

                        foreach (var hop in data["lsp"]["rro"])
                        {
                            hops.Add((string)hop["address"]);
                        }

                        l.RecordRouteObject = hops.ToArray();
                    }

                    this.Update(l);
                }
            }
        }
    }
}