namespace shift.yggdrasil2.Topology.MPLS.LabelSwitchedPath
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using J = Newtonsoft.Json.JsonPropertyAttribute;

    [Serializable]
    public partial class LabelSwitchedPath
    {
        [J("LastEvent")] public long LastEvent { get; set; }
        [J("FirstSeen")] public long FirstSeen { get; set; }
        [J("ReservedBandwidth")] public long? ReservedBandwidth { get; set; }
        [J("Remove")] public bool? Remove { get; set; }
        [J("ExtendedTunnelIdentifier")] public string ExtendedTunnelIdentifier { get; set; }
        [J("ExtendedTunnelIdentifierTunnelId")] public string ExtendedTunnelIdentifierTunnelId { get; set; }
        [J("IPv4TunnelEndpointAddress")] public string IPv4TunnelEndpointAddress { get; set; }
        [J("LSPIdentifier")] public string LspIdentifier { get; set; }
        [J("Administrative")] public bool Administrative { get; set; }
        [J("Delegate")] public bool? Delegate { get; set; }
        [J("Operational")] public bool Operational { get; set; }
        [J("TunnelIdentifier")] public string TunnelIdentifier { get; set; }
        [J("SymbolicPathName")] public string SymbolicPathName { get; set; }
        [J("RecordRouteObject")] public string[] RecordRouteObject { get; set; }
        [J("ComputedExplicitRouteObject")] public string[] ComputedExplicitRouteObject { get; set; }
        [J("ComputedExplicitRouteObjectBaseline")] public string[] ComputedExplicitRouteObjectBaseline { get; set; }
        [J("Sync")] public bool? Sync { get; set; }
        [J("IPv4TunnelSenderAddress")] public string IPv4TunnelSenderAddress { get; set; }
        [J("ClassOfService")] public int? ClassOfService { get; set; }
        [J("PCC")] public string PCC { get; set; }
        [J("Parent_Id")] public string ParentId { get; }
        [J("Standby")] public bool Standby { get; set; }
        [J("Delete")] public bool Delete { get; set; }
        [J("Feasible")] public bool Feasible { get; set; }
        [J("Optimise")] public bool Optimise { get; set; }

        public bool IsChild { get { return !string.IsNullOrWhiteSpace(this.ParentId); } }
        public string Id { get { return this.IPv4TunnelSenderAddress + "+" + this.IPv4TunnelEndpointAddress + "/" + this.SymbolicPathName; } }

        public LabelSwitchedPath(string ParentId)
        {
            this.Standby = false;
            this.Feasible = false;
            this.Delete = false;
            this.ParentId = ParentId;
            this.RecordRouteObject = new string[0];
            this.ComputedExplicitRouteObject = new string[0];
            this.ComputedExplicitRouteObjectBaseline = new string[0];
        }
    }

    public partial class LabelSwitchedPath
    {
        public static LabelSwitchedPath FromJson(string json) => JsonConvert.DeserializeObject<LabelSwitchedPath>(json, shift.yggdrasil2.Topology.MPLS.LabelSwitchedPath.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this LabelSwitchedPath self) => JsonConvert.SerializeObject(self, shift.yggdrasil2.Topology.MPLS.LabelSwitchedPath.Converter.Settings);
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

