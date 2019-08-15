using System;
using System.IO;

#region JSON
using Newtonsoft.Json;
#endregion

namespace shift.ui.architect
{
    class Config
    {
        public string MessageBrokerHostname { get; set; }
        public string MessageBrokerUsername { get; set; }
        public string MessageBrokerPassword { get; set; }
        public int MessageBrokerPort { get; set; }

        public string IntentQueueRoutingKey { get; set; }
        public string PerformanceQueueRoutingKey { get; set; }
        public string ControlQueueRoutingKey { get; set; }
        public string IGPTopologyQueueRoutingKey { get; set; }
        public string MPLSTopologyQueueRoutingKey { get; set; }
        public string NodePCCQueueRoutingKey { get; set; }

        public string Neo4J_URI { get; set; }
        public string Neo4J_User { get; set; }
        public string Neo4J_Password { get; set; }

        public string IntentRepositoryURL { get; set; }

        public string SDNControllerURI { get; set; }
        public string SDNControllerUsername { get; set; }
        public string SDNControllerPassword { get; set; }

        public string AnsibleTowerURI { get; set; }
        public string AnsibleTowerUsername { get; set; }
        public string AnsibleTowerPassword { get; set; }

        public Config() { }

        public static Config LoadFromFile(string filename)
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(filename));
        }

        public static Config LoadFromEnvironment(string filename)
        {
            var c = new Config();
            c = LoadFromFile(filename); // Load defaults

            var messageBrokerHostname = Environment.GetEnvironmentVariable("rmq_hostname");
            if (!string.IsNullOrWhiteSpace(messageBrokerHostname)) { c.MessageBrokerHostname = messageBrokerHostname; }

            var messageBrokerUsername = Environment.GetEnvironmentVariable("rmq_username");
            if (!string.IsNullOrWhiteSpace(messageBrokerUsername)) { c.MessageBrokerUsername = messageBrokerUsername; }

            var messageBrokerPassword = Environment.GetEnvironmentVariable("rmq_password");
            if (!string.IsNullOrWhiteSpace(messageBrokerPassword)) { c.MessageBrokerPassword = messageBrokerPassword; }

            var messageBrokerPort = Environment.GetEnvironmentVariable("rmq_port");
            if (!string.IsNullOrWhiteSpace(messageBrokerPort)) { if (int.TryParse(messageBrokerPort, out int val)) { c.MessageBrokerPort = val; } }


            var intentQueueRoutingKey = Environment.GetEnvironmentVariable("shift_intent_routing_key");
            if (!string.IsNullOrWhiteSpace(intentQueueRoutingKey)) { c.IntentQueueRoutingKey = intentQueueRoutingKey; }

            var performanceQueueRoutingKey = Environment.GetEnvironmentVariable("shift_rpm_routing_key");
            if (!string.IsNullOrWhiteSpace(performanceQueueRoutingKey)) { c.PerformanceQueueRoutingKey = performanceQueueRoutingKey; }

            var controlQueueRoutingKey = Environment.GetEnvironmentVariable("shift_control_routing_key");
            if (!string.IsNullOrWhiteSpace(controlQueueRoutingKey)) { c.ControlQueueRoutingKey = controlQueueRoutingKey; }

            var igpTopologyQueueRoutingKey = Environment.GetEnvironmentVariable("shift_links_topology_routing_key");
            if (!string.IsNullOrWhiteSpace(igpTopologyQueueRoutingKey)) { c.IGPTopologyQueueRoutingKey = igpTopologyQueueRoutingKey; }

            var mplsTopologyQueueRoutingKey = Environment.GetEnvironmentVariable("shift_lsp_topology_routing_key");
            if (!string.IsNullOrWhiteSpace(mplsTopologyQueueRoutingKey)) { c.MPLSTopologyQueueRoutingKey = mplsTopologyQueueRoutingKey; }

            var nodePCCQueueRoutingKey = Environment.GetEnvironmentVariable("shift_pcc_routing_key");
            if (!string.IsNullOrWhiteSpace(nodePCCQueueRoutingKey)) { c.NodePCCQueueRoutingKey = nodePCCQueueRoutingKey; }


            var neo4J_Host = Environment.GetEnvironmentVariable("neo4j_hostname");
            var neo4J_Port = Environment.GetEnvironmentVariable("neo4j_bolt_port");
            if (!string.IsNullOrWhiteSpace(neo4J_Host) && !string.IsNullOrWhiteSpace(neo4J_Port)) { c.Neo4J_URI = "bolt://" + neo4J_Host + ":" + neo4J_Port + "/"; }

            var neo4J_User = Environment.GetEnvironmentVariable("neo4j_username");
            if (!string.IsNullOrWhiteSpace(neo4J_User)) { c.Neo4J_User = neo4J_User; }

            var neo4J_Password = Environment.GetEnvironmentVariable("neo4j_password");
            if (!string.IsNullOrWhiteSpace(neo4J_Password)) { c.Neo4J_Password = neo4J_Password; }


            var intentRepositoryURL = Environment.GetEnvironmentVariable("shift_repository_url");
            if (!string.IsNullOrWhiteSpace(intentRepositoryURL)) { c.IntentRepositoryURL = intentRepositoryURL; }


            var sdnControllerHostname = Environment.GetEnvironmentVariable("odl_vip");
            if (!string.IsNullOrWhiteSpace(sdnControllerHostname)) { c.SDNControllerURI = "http://" + sdnControllerHostname + ":8181"; }

            var sdnControllerUsername = Environment.GetEnvironmentVariable("odl_user");
            if (!string.IsNullOrWhiteSpace(sdnControllerUsername)) { c.SDNControllerUsername = sdnControllerUsername; }

            var sdnControllerPassword = Environment.GetEnvironmentVariable("odl_password");
            if(!string.IsNullOrWhiteSpace(sdnControllerPassword)) { c.SDNControllerPassword = sdnControllerPassword; }


            var ansibleTowerURI = Environment.GetEnvironmentVariable("tower_uri");
            if (!string.IsNullOrWhiteSpace(ansibleTowerURI)) { c.AnsibleTowerURI = ansibleTowerURI; }

            var ansibleTowerUsername = Environment.GetEnvironmentVariable("tower_user");
            if (!string.IsNullOrWhiteSpace(ansibleTowerUsername)) { c.AnsibleTowerUsername = ansibleTowerUsername; }
            
            var ansibleTowerPassword = Environment.GetEnvironmentVariable("tower_password");
            if (!string.IsNullOrWhiteSpace(ansibleTowerPassword)) { c.AnsibleTowerPassword = ansibleTowerPassword; }

            return c;

        }
    }
}
