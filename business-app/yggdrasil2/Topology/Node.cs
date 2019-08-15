namespace shift.yggdrasil2.Topology.Node
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using J = Newtonsoft.Json.JsonPropertyAttribute;

    [Serializable]
    public partial class Node
    {
        [J("topology_id")] public string TopologyId { get; set; }
        [J("Operational_Status")] public bool OperationalStatus { get; set; }
        [J("Last_Event")] public long? LastEvent { get; set; }
        [J("Node_Name")] public string NodeName { get; set; }
        [J("Is_Pseudonode")] public bool IsPseudonode { get; set; }
        [J("Node_Cost")] public long NodeCost { get; set; }
        [J("IGP_Router_Identifier")] public string IgpRouterIdentifier { get; set; }
        [J("psn")] public int? Psn { get; set; }
        [J("PCC")] public string PCC { get; set; }
        [J("IPv4_Router_Identifier")] public string IPv4RouterIdentifier { get; set; }
        [J("First_Seen")] public long? FirstSeen { get; set; }

        public string Id
        {
            get
            {
                switch (TopologyId)
                {
                    case "igp":
                        return this.IgpRouterIdentifier;
                    case "mpls":
                        return this.IPv4RouterIdentifier;                   
                    default:
                        return "";
                }
            }
        }
    }

    public partial class Node
    {
        public static Node FromJson(string json) => JsonConvert.DeserializeObject<Node>(json, shift.yggdrasil2.Topology.Node.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Node self) => JsonConvert.SerializeObject(self, shift.yggdrasil2.Topology.Node.Converter.Settings);
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
