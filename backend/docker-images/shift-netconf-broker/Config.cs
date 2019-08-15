using Newtonsoft.Json;
using System.IO;

namespace shift_netconf_broker
{
    public class RESTServerConfig
    {

        [JsonProperty("Host")]
        public string Host { get; set; }

        [JsonProperty("Port")]
        public int Port { get; set; }

    }

    public class ReadOnly
    {
        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Password")]
        public string Password { get; set; }
    }

    public class ReadWrite
    {
        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Password")]
        public string Password { get; set; }
    }

    public class ServerBasicAuth
    {
        [JsonProperty("ReadOnly")]
        public ReadOnly ReadOnly { get; set; }

        [JsonProperty("ReadWrite")]
        public ReadWrite ReadWrite { get; set; }
    }

    public class InventoryAuth
    {
        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Password")]
        public string Password { get; set; }
    }

    public class NodeAuth
    {
        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Password")]
        public string Password { get; set; }
    }

    public class Inventory
    {
        [JsonProperty("LoadNodesOnStartUp")]
        public bool LoadNodesOnStartUp { get; set; }

        [JsonProperty("InventoryReloadInterval")]
        public int InventoryReloadInterval { get; set; }

        [JsonProperty("InventoryURI")]
        public string InventoryURI { get; set; }

        [JsonProperty("InventoryResource")]
        public string InventoryResource { get; set; }

        [JsonProperty("InventoryLimit")]
        public int InventoryLimit { get; set; }

        [JsonProperty("InventoryAuth")]
        public InventoryAuth InventoryAuth { get; set; }

        [JsonProperty("InventoryBlackListRegex")]
        public string InventoryBlackListRegex { get; set; }

        [JsonProperty("NodeAuth")]
        public NodeAuth NodeAuth { get; set; }

        [JsonProperty("NodeKeepAliveInterval")]
        public int NodeKeepAliveInterval { get; set; }
    }

    public class MessageBroker
    {
        [JsonProperty("MessageBrokerHost")]
        public string MessageBrokerHost { get; set; }

        [JsonProperty("MessageBrokerUsername")]
        public string MessageBrokerUsername { get; set; }

        [JsonProperty("MessageBrokerPassword")]
        public string MessageBrokerPassword { get; set; }

        [JsonProperty("ExchangeName")]
        public string ExchangeName { get; set; }

        [JsonProperty("RPCRequestQueueSuffix")]
        public string RPCRequestQueueSuffix { get; set; }

        [JsonProperty("RPCRoutingKeyPrefix")]
        public string RPCRoutingKeyPrefix { get; set; }
    }

    public class Config
    {
        [JsonProperty("RESTServerConfig")]
        public RESTServerConfig RESTServerConfig { get; set; }

        [JsonProperty("ServerBasicAuth")]
        public ServerBasicAuth ServerBasicAuth { get; set; }

        [JsonProperty("Inventory")]
        public Inventory Inventory { get; set; }

        [JsonProperty("MessageBroker")]
        public MessageBroker MessageBroker { get; set; }

        public static Config getConfig()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "config.json")));

            return config;
        }
    }
}