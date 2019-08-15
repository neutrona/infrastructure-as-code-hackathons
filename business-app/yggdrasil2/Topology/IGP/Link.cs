namespace shift.yggdrasil2.Topology.IGP.Link
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.ComponentModel;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using J = Newtonsoft.Json.JsonPropertyAttribute;

    [Serializable]
    public partial class Link
    {
        [J("Source_Node")] public string SourceNode { get; set; }
        [J("Target_Node")] public string TargetNode { get; set; }
        [J("Link_Cost")] public double LinkCost { get; set; }
        [J("Operational_Status")] public bool OperationalStatus { get; set; }        
        [J("Shared_Risk_Link_Groups")] public long[] SharedRiskLinkGroups { get; set; }
        [J("rtt")] public long? Rtt { get; set; }
        [J("Unreserved_Bandwidth")] public double[] UnreservedBandwidth { get; set; }
        [J("Last_Event")] public long? LastEvent { get; set; }
        [J("Maximum_Link_Bandwidth")] public double? MaximumLinkBandwidth { get; set; }
        [J("Maximum_Reservable_Link_Bandwidth")] public double? MaximumReservableLinkBandwidth { get; set; }
        [J("First_Seen")] public long? FirstSeen { get; set; }
        [J("IPv4_Interface_Address")] public string IPv4InterfaceAddress { get; set; }
        [J("asn")] public long? Asn { get; set; }

        // Sample Properties
        [J("MTU")] public int? MTU { get; set; }
        [J("MRU")] public int? MRU { get; set; }
        [J("Errors")] public long? Errors { get; set; }
        [J("PacketLoss")] public long? PacketLoss { get; set; }
        [J("Drops")] public long? Drops { get; set; }

        // Custom Long Cost
        [J("CustomLongCost")] public long? CustomLongCost { get; set; }

        // Custom Double Cost
        [J("CustomDoubleCost")] public double? CustomDoubleCost { get; set; }

        public string Id { get { return this.SourceNode + "+" + this.TargetNode; } }
        public string ReverseId { get { return this.TargetNode + "+" + this.SourceNode; } }

        public Link()
        {
            this.SharedRiskLinkGroups = new long[0];
        }
    }

    public partial class Link
    {
        public static Link FromJson(string json) => JsonConvert.DeserializeObject<Link>(json, shift.yggdrasil2.Topology.IGP.Link.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Link self) => JsonConvert.SerializeObject(self, shift.yggdrasil2.Topology.IGP.Link.Converter.Settings);
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
