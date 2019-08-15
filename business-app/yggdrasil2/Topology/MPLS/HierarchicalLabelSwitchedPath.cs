using System;
using System.Collections.Generic;
using System.Text;

namespace shift.yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using J = Newtonsoft.Json.JsonPropertyAttribute;

    [Serializable]
    public partial class HierarchicalLabelSwitchedPath
    {
        [J("IPv4TunnelSenderAddress")] public string IPv4TunnelSenderAddress { get; set; }
        [J("IPv4TunnelEndpointAddress")] public string IPv4TunnelEndpointAddress { get; set; }
        [J("SymbolicPathName")] public string SymbolicPathName { get; set; }
        [J("FastReroute")] public bool FastReroute { get; set; }

        [J("Configure")] public bool Configure { get; set; }
        [J("UpdateConfiguration")] public bool UpdateConfiguration { get; set; }

        [J("Delete")] public bool Delete { get; set; }

        [J("PCC")] public string PCC { get; set; }

        [J("AnsibleTowerJobTemplateId")] public int AnsibleTowerJobTemplateId { get; set; }
        [J("AnsibleTowerHost")] public string AnsibleTowerHost { get; set; }

        [J("Children")] public List<LabelSwitchedPath.LabelSwitchedPath> Children { get; set; }

        public string Id { get { return this.IPv4TunnelSenderAddress + "+" + this.IPv4TunnelEndpointAddress + "/" + this.SymbolicPathName; } }

        public HierarchicalLabelSwitchedPath(string SymbolicPathName)
        {
            this.SymbolicPathName = SymbolicPathName;
            this.FastReroute = false;

            this.UpdateConfiguration = false;
            this.Delete = false;

            this.AnsibleTowerJobTemplateId = -1;

            this.Children = new List<LabelSwitchedPath.LabelSwitchedPath>();
        }
    }

    public partial class HierarchicalLabelSwitchedPath
    {
        public static HierarchicalLabelSwitchedPath FromJson(string json) => JsonConvert.DeserializeObject<HierarchicalLabelSwitchedPath>(json, shift.yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this HierarchicalLabelSwitchedPath self) => JsonConvert.SerializeObject(self, shift.yggdrasil2.Topology.MPLS.HierarchicalLabelSwitchedPath.Converter.Settings);
    }

    internal class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

}
