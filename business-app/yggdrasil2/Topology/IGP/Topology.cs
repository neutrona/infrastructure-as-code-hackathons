using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#region JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endregion


namespace shift.yggdrasil2.Topology.IGP
{
    public class Topology
    {
        public delegate void OnLinkUpdateHandler(Link.Link link);
        public event OnLinkUpdateHandler OnLinkUpdateCallback;

        public delegate void OnNodeUpdateHandler(Node.Node node);
        public event OnNodeUpdateHandler OnNodeUpdateCallback;

        public delegate void OnLinkDownHandler(Link.Link link);
        public event OnLinkDownHandler OnLinkDownCallback;

        public delegate void OnLinkUpHandler(Link.Link link);
        public event OnLinkUpHandler OnLinkUpCallback;

        public const string TopologyId = "igp";
        public List<Node.Node> Nodes { get; }
        public List<Link.Link> Links { get; }

        public bool locked { get; private set; }

        public Topology()
        {
            this.Nodes = new List<Node.Node>();
            this.Links = new List<Link.Link>();
        }

        public async void Clear()
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            this.Links.Clear();
            this.Nodes.Clear();

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


            Node.Node existingNode = Nodes.Find(n => n.IgpRouterIdentifier == Node.IgpRouterIdentifier);

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
                    Nodes.Remove(existingNode);
                }
            }

            this.OnNodeUpdateCallback?.Invoke(Node);

            this.locked = false;
        }

        public async void Update(Link.Link Link)
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            Link.Link existingLink = Links.Find(l => l.SourceNode == Link.SourceNode && l.TargetNode == Link.TargetNode); // && l.IPv4InterfaceAddress == Link.IPv4InterfaceAddress);

            if (existingLink == null && Link.OperationalStatus)
            {
                Links.Add(Link);

                this.OnLinkUpCallback?.Invoke(Link);
            }
            else
            {
                if (Link.OperationalStatus)
                {
                    Links[Links.IndexOf(existingLink)] = Link;
                }
                else
                {
                    Links.Remove(existingLink);

                    this.OnLinkDownCallback?.Invoke(Link);
                }

            }

            this.OnLinkUpdateCallback?.Invoke(Link);

            this.locked = false;
        }

        public void Update(JObject data)
        {
            switch ((string)data["yggdrasil-data-type"])
            {
                case "exabgp-bgp-ls-message":
                    ProcessExaBgpBgpLsMessage(data);
                    break;
                case "performance-rpm-message":
                    ProcessRpmMessage(data);
                    break;
                case "node-pcc-message":
                    ProcessNodePCCMessage(data);
                    break;
                default:
                    // Unknown data type
                    break;
            }

        }

        private void ProcessExaBgpBgpLsMessage(JObject data)
        {
            if ((string)data["type"] == "update")
            {
                var withdraw = data["neighbor"]["message"]["update"]["withdraw"];
                if (withdraw != null)
                {
                    foreach (var item in withdraw["bgp-ls bgp-ls"])
                    {
                        switch ((int)item["ls-nlri-type"])
                        {
                            case 1:

                                string psn = (string)item["node-descriptors"]["psn"];
                                string igp_router_identifier = (string)item["node-descriptors"]["router-id"] + psn;


                                Node.Node n = new Node.Node()
                                {
                                    IgpRouterIdentifier = igp_router_identifier,
                                    OperationalStatus = false,
                                    IsPseudonode = !string.IsNullOrWhiteSpace(psn)
                                };

                                this.Update(n);

                                break;
                            case 2:
                                string local_node_id = (string)item["local-node-descriptors"]["router-id"] + (string)item["local-node-descriptors"]["psn"];
                                string remote_node_id = (string)item["remote-node-descriptors"]["router-id"] + (string)item["remote-node-descriptors"]["psn"];

                                Link.Link l = new Link.Link()
                                {
                                    SourceNode = local_node_id,
                                    TargetNode = remote_node_id,
                                    OperationalStatus = false
                                };

                                this.Update(l);

                                break;
                            default:
                                break;
                        }
                    }
                }

                var announce = data["neighbor"]["message"]["update"]["announce"];
                if (announce != null)
                {
                    foreach (var item in announce["bgp-ls bgp-ls"].First.Children())
                    {
                        var attribute = data["neighbor"]["message"]["update"]["attribute"];

                        foreach (var subitem in item.Children())
                        {
                            switch ((int)subitem["ls-nlri-type"])
                            {
                                case 1:

                                    string psn = (string)subitem["node-descriptors"]["psn"];
                                    string igp_router_identifier = (string)subitem["node-descriptors"]["router-id"] + psn;

                                    Node.Node n = new Node.Node()
                                    {
                                        IgpRouterIdentifier = igp_router_identifier,
                                        OperationalStatus = true,
                                        IsPseudonode = !string.IsNullOrWhiteSpace(psn)
                                    };

                                    if (attribute != null && attribute["bgp-ls"] != null)
                                    {
                                        n.NodeName = (string)attribute["bgp-ls"]["node-name"];
                                        n.IPv4RouterIdentifier = (string)attribute["bgp-ls"]["local-te-router-id"];
                                    }

                                    int psn_number = 0;
                                    int.TryParse(psn, out psn_number);
                                    n.Psn = psn_number;

                                    if (n.IsPseudonode) { n.NodeCost = 0; } else { n.NodeCost = 1; }

                                    this.Update(n);

                                    break;
                                case 2:
                                    string local_node_id = (string)subitem["local-node-descriptors"]["router-id"] + (string)subitem["local-node-descriptors"]["psn"];
                                    string remote_node_id = (string)subitem["remote-node-descriptors"]["router-id"] + (string)subitem["remote-node-descriptors"]["psn"];
                                    string interface_address = (string)subitem["interface-address"]["interface-address"];

                                    if (!string.IsNullOrWhiteSpace(interface_address))
                                    {
                                        Link.Link l = new Link.Link()
                                        {
                                            SourceNode = local_node_id,
                                            TargetNode = remote_node_id,
                                            IPv4InterfaceAddress = interface_address,
                                            OperationalStatus = true,
                                            Rtt = long.MaxValue
                                        };

                                        if (attribute != null && attribute["bgp-ls"] != null)
                                        {
                                            l.MaximumLinkBandwidth = (double?)attribute["bgp-ls"]["maximum-link-bandwidth"];
                                            l.MaximumReservableLinkBandwidth = (double?)attribute["bgp-ls"]["maximum-reservable-link-bandwidth"];
                                            l.UnreservedBandwidth = attribute["bgp-ls"]["unreserved-bandwidth"]?.Values<double>().ToArray();
                                            l.SharedRiskLinkGroups = attribute["bgp-ls"]["shared-risk-link-groups"]?.Values<long>().ToArray();
                                        }

                                        if (Regex.IsMatch(l.SourceNode, @"^\d{12}\d+$") || Regex.IsMatch(l.TargetNode, @"^\d{12}\d+$"))
                                        {
                                            l.LinkCost = 0.5;
                                        }
                                        else
                                        {
                                            l.LinkCost = 1;
                                        }

                                        this.Update(l);
                                    }

                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async void ProcessRpmMessage(JObject data)
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            string source_address = (string)data["source_address"];
            long rtt = (long)data["rtt"];

            var existingLink = Links.Find(l => l.IPv4InterfaceAddress == source_address);

            if (existingLink != null)
            {
                existingLink.Rtt = rtt;
            }

            this.locked = false;
        }

        private async void ProcessNodePCCMessage(JObject data)
        {
            while (this.locked)
            {
                await Task.Delay(1);
            }

            this.locked = true;

            string ipv4_router_identifier = (string)data["IPv4_Router_Identifier"];
            string local_ip = (string)data["local_ip"];

            var existingNode = Nodes.Find(n => n.IPv4RouterIdentifier == ipv4_router_identifier);

            if (existingNode != null)
            {
                existingNode.PCC = local_ip;
            }

            this.locked = false;
        }
    }
}
