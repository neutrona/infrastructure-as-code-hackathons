using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;

#region RestSharp
using RestSharp;
using RestSharp.Authenticators;
#endregion

#region JSON.NET
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
#endregion

#region SSH.NET
using Renci.SshNet;
#endregion

#region RabbitMQ
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
#endregion

namespace shift_netconf_broker
{
    public class InventoryClient
    {

        public List<Node> Nodes { get; set; }

        public Config GlobalConfiguration { get; set; } // TODO: Replace below properties with this Config object
        public string InventoryURI { get; set; }
        public string InventoryResource { get; set; }
        public int InventoryLimit { get; set; }
        public string InventoryUsername { get; set; }
        public string InventoryPassword { get; set; }
        public string InventoryBlackListRegex { get; set; }

        public string NodeUsername { get; set; }
        public string NodePassword { get; set; }
        public int NodeReconnectInterval { get; set; }

        public string MessageBrokerHost { get; set; }
        public string MessageBrokerUsername { get; set; }
        public string MessageBrokerPassword { get; set; }
        public string ExchangeName { get; set; }
        public string RPCRequestQueueSuffix { get; set; }
        public string RPCRoutingKeyPrefix { get; set; }

        public void LoadInventory()
        {
            List<Node> nodes = new List<Node>();

            var client = new RestClient(this.InventoryURI);
            client.Authenticator = new HttpBasicAuthenticator(this.InventoryUsername, this.InventoryPassword);
            var request = new RestRequest(this.InventoryResource);

            request.AddParameter("limit", this.InventoryLimit);

            IRestResponse response = client.Execute(request);
            var content = response.Content;

            JArray a = JArray.Parse(content);

            foreach (JObject o in a.Children<JObject>())
            {
                var nodeNameJObject = o.SelectToken("$..Node_Name");
                var nodeIPv4IdJObject = o.SelectToken("$..IPv4_Router_Identifier");

                if (nodeNameJObject != null && nodeIPv4IdJObject != null)
                {
                    string Node_Name = nodeNameJObject.Value<string>();
                    string IPv4_Router_Identifier = nodeIPv4IdJObject.Value<string>();

                    // foreach (var replaceRegex in this.GlobalConfiguration.Inventory.IPv4RouterIdentifierReplaceRegex)
                    // {
                    //     IPv4_Router_Identifier = Regex.Replace(IPv4_Router_Identifier, replaceRegex.Match, replaceRegex.ReplaceWith);
                    // }

                    Node n = new Node(IPv4_Router_Identifier,
                                      this.NodeUsername,
                                      this.NodePassword,
                                      this.NodeReconnectInterval,
                                      this.MessageBrokerHost,
                                      this.MessageBrokerUsername,
                                      this.MessageBrokerPassword,
                                      this.ExchangeName,
                                      this.RPCRequestQueueSuffix,
                                      this.RPCRoutingKeyPrefix);

                    n.Node_Name = Regex.Replace(Node_Name, @"-re.$", "");

                    if (!Regex.IsMatch(n.Node_Name, this.InventoryBlackListRegex))
                    {
                        nodes.Add(n);
                    } 
                }
            }

            Console.WriteLine("Loaded {0} nodes.", nodes.Count);

            // Add new nodes to local inventory
            foreach (var node in nodes)
            {
                if (!IsNodeInList(node, this.Nodes))
                {
                    this.Nodes.Add(node);
                }
            }

            // Remove nodes that do not exist in remote inventory
            foreach (var node in this.Nodes)
            {
                if (!IsNodeInList(node, nodes))
                {
                    if (node.IsConnected)
                    {
                        node.Disconnect();
                    }

                    this.Nodes.Remove(node);
                }
            }

            // this.Nodes = this.Nodes.Skip(20).ToList(); // TESTING
        }

        public void ConnectInventory()
        {
            int i = 0;
            foreach (var node in this.Nodes)
            {
                i++;
                Console.Write("\rConnect {0} of {1}      ", i, this.Nodes.Count);

                try
                {
                    if (!node.IsConnected)
                    {
                        node.Connect();
                    }
                }
                catch (Exception ex)
                {
                    node.LastExceptionMessage = ex.Message;
                }
            }
        }

        public void DisconnectInventory()
        {
            int i = 0;
            foreach (var node in this.Nodes)
            {
                i++;
                Console.Write("\rDisconnect {0} of {1}      ", i, this.Nodes.Count);
                try
                {
                    if (node.IsConnected)
                    {
                        node.Shutdown();
                    }                    
                }
                catch (Exception ex)
                {
                    node.LastExceptionMessage = ex.Message;
                }
            }
        }

        public InventoryClient(string InventoryURI,
                               string InventoryResource,
                               int InventoryLimit,
                               string InventoryUsername,
                               string InventoryPassword,
                               string InventoryBlackListRegex,
                               int InventoryReloadInterval,
                               string NodeUsername,
                               string NodePassword,
                               int NodeReconnectInterval,
                               string MessageBrokerHost,
                               string MessageBrokerUsername,
                               string MessageBrokerPassword,
                               string ExchangeName,
                               string RPCRequestQueueSuffix,
                               string RPCRoutingKeyPrefix,
                               Config GlobalConfig)
        {
            this.InventoryURI = InventoryURI;
            this.InventoryResource = InventoryResource;
            this.InventoryLimit = InventoryLimit;
            this.InventoryUsername = InventoryUsername;
            this.InventoryPassword = InventoryPassword;
            this.InventoryBlackListRegex = InventoryBlackListRegex;
            this.NodeUsername = NodeUsername;
            this.NodePassword = NodePassword;
            this.NodeReconnectInterval = NodeReconnectInterval;
            this.MessageBrokerHost = MessageBrokerHost;
            this.MessageBrokerUsername = MessageBrokerUsername;
            this.MessageBrokerPassword = MessageBrokerPassword;
            this.ExchangeName = ExchangeName;
            this.RPCRequestQueueSuffix = RPCRequestQueueSuffix;
            this.RPCRoutingKeyPrefix = RPCRoutingKeyPrefix;
            this.GlobalConfiguration = GlobalConfig;

            this.Nodes = new List<Node>();
        }

        private bool IsNodeInList(Node node, List<Node> list)
        {
            var existingNode = list.Where(n => n.IPv4_Router_Identifier == node.IPv4_Router_Identifier).FirstOrDefault();

            return (existingNode == null) ? false : true;
        }
    }

    public class Node
    {
        [JsonProperty("Node_Name")]
        public string Node_Name { get; set; }

        [JsonProperty("IPv4_Router_Identifier")]
        public string IPv4_Router_Identifier { get; set; }

        [JsonIgnore]
        public string NodeUsername { get; set; }

        [JsonIgnore]
        public string NodePassword { get; set; }

        [JsonProperty("Node_Keep_Alive_Interval")]
        public int NodeKeepAliveInterval { get; set; }

        [JsonProperty("Last_Exception_Message")]
        public string LastExceptionMessage { get; set; }

        [JsonProperty("Is_Connecting")]
        public bool IsConnecting { get; set; }

        [JsonProperty("Is_Connected")]
        public bool IsConnected { get { return this.NetConfClient.IsConnected; } }

        [JsonProperty("Is_Busy")]
        public bool IsBusy { get; set; }

        [JsonProperty("Retries")]
        public int Retries { get; set; }

        [JsonProperty("Message_Broker_Host")]
        public string MessageBrokerHost { get; set; }

        [JsonIgnore]
        public string MessageBrokerUsername { get; set; }
        [JsonIgnore]
        public string MessageBrokerPassword { get; set; }

        [JsonProperty("Exchange_Name")]
        public string ExchangeName { get; set; }
        [JsonProperty("RPC_Request_Queue_Suffix")]
        public string RPCRequestQueueSuffix { get; set; }
        [JsonProperty("RPC_Routing_Key_Prefix")]
        public string RPCRoutingKeyPrefix { get; set; }

        [JsonProperty("RPC_Request_Queue_Name")]
        public string RPCRequestQueueName { get { return this.Node_Name + this.RPCRequestQueueSuffix; } }
        [JsonProperty("RPC_Routing_Key")]
        public string RPCRoutingKey { get { return this.RPCRoutingKeyPrefix + this.Node_Name; } }

        [JsonIgnore]
        IConnection MessageBrokerConnection { get; set; }
        [JsonIgnore]
        IModel MessageBrokerChannel { get; set; }

        [JsonProperty("Is_Message_Broker_Channel_Open")]
        public bool IsMessageBrokerChannelOpen {
            get { if (this.MessageBrokerChannel != null){ return this.MessageBrokerChannel.IsOpen; } else{ return false; } }
        }

        [JsonIgnore]
        public NetConfClient NetConfClient { get; set; }

        private System.Timers.Timer ConnectionCheckTimer;

        public void Connect()
        {
            this.IsConnecting = true;
            this.NetConfClient.Connect();
            this.IsConnecting = false;

            this.ConnectionCheckTimer.Start();

            if (this.IsConnected)
            {
                var factory = new ConnectionFactory()
                {
                    HostName = this.MessageBrokerHost,
                    UserName = this.MessageBrokerUsername,
                    Password = this.MessageBrokerPassword
                };

                this.MessageBrokerConnection = factory.CreateConnection();
                this.MessageBrokerConnection.AutoClose = false;

                this.MessageBrokerChannel = this.MessageBrokerConnection.CreateModel();

                this.MessageBrokerChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                this.MessageBrokerChannel.ExchangeDeclare(exchange: this.ExchangeName,
                                                              type: "topic",
                                                              durable: false,
                                                              autoDelete: true,
                                                              arguments: null);

                this.MessageBrokerChannel.QueueDeclare(queue: this.RPCRequestQueueName,
                                                           durable: false,
                                                           exclusive: false,
                                                           autoDelete: true,
                                                           arguments: new Dictionary<string, object>(){{"x-ha-policy", "all"}});

                this.MessageBrokerChannel.QueueBind(queue: this.RPCRequestQueueName,
                                                        exchange: this.ExchangeName,
                                                        routingKey: this.RPCRoutingKey);

                var consumer = new EventingBasicConsumer(this.MessageBrokerChannel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;

                    var props = ea.BasicProperties;
                    var correlationId = props.CorrelationId;
                    var replyTo = props.ReplyTo;

                    if (!string.IsNullOrWhiteSpace(correlationId) && !string.IsNullOrWhiteSpace(replyTo))
                    {
                        var replyProps = this.MessageBrokerChannel.CreateBasicProperties();
                        replyProps.CorrelationId = correlationId;

                        Console.WriteLine("\n [x] Received '{0}':'{1}' \n Reply To: '{2}':'{3}'",
                                          routingKey,
                                          message,
                                          replyTo,
                                          correlationId);

                        var response = this.ExecuteReadRPC(message);

                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        this.MessageBrokerChannel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo,
                          basicProperties: replyProps, body: responseBytes);
                    }
                    else
                    {
                        Console.WriteLine("\n [-] Empty Correlation_Id or Reply_To parameters.");
                    }

                    this.MessageBrokerChannel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                this.MessageBrokerChannel.BasicConsume(queue: this.RPCRequestQueueName,
                                     noAck: false,
                                     consumer: consumer);
            }
        }

        public void RestartSession()
        {
            this.ConnectionCheckTimer.Stop();
            this.NetConfClient.Disconnect();
            this.NetConfClient.Connect();
            this.IsBusy = false;
        }

        public void Disconnect()
        {
            this.MessageBrokerChannel.Close();
            this.MessageBrokerConnection.Close();

            this.NetConfClient.Disconnect();
            this.IsBusy = false;
        }

        public void Shutdown()
        {
            this.ConnectionCheckTimer.Stop();

            this.MessageBrokerChannel.Close();
            this.MessageBrokerConnection.Close();

            this.NetConfClient.Disconnect();
            this.IsBusy = false;
        }

        public string ExecuteReadRPC(string xml_rpc)
        {
            if (this.IsConnected && !this.IsBusy)
            {
                try
                {
                    XmlDocument xmlDocRequest = new XmlDocument();
                    xmlDocRequest.PreserveWhitespace = false;
                    xmlDocRequest.LoadXml(xml_rpc);

                    if (IsValidRPC(xmlDocRequest))
                    {
                        this.IsBusy = true;
                        var result = this.NetConfClient.SendReceiveRpc(xmlDocRequest);
                        this.IsBusy = false;
                        this.Retries = 0;
                        return "<node><node-ipv4-router-id>" + this.IPv4_Router_Identifier + "</node-ipv4-router-id><node-name>" + this.Node_Name + "</node-name><result>" + result.InnerXml + "</result></node>";
                    }
                    else
                    {
                        return "<node><node-ipv4-router-id>" + this.IPv4_Router_Identifier + "</node-ipv4-router-id><node-name>" + this.Node_Name + "</node-name><error>Malformed, invalid or too broad RPC.</error></node>";
                    }

                }
                catch (Exception ex)
                {
                    if (ex.Message == "The session is not open." && this.Retries < 3)
                    {
                        this.Retries++;
                        this.RestartSession();
                        return ExecuteReadRPC(xml_rpc);
                    }

                    this.IsBusy = false;
                    this.LastExceptionMessage = ex.Message;
                    return "<node><node-ipv4-router-id>" + this.IPv4_Router_Identifier + "</node-ipv4-router-id><node-name>" + this.Node_Name + "</node-name><error>" + ex.Message + "</error></node>";
                }
            }
            else
            {
                string errorMessage = "Invalid State";

                if (!this.IsConnected)
                {
                    errorMessage = "Not Connected";
                }
                else
                {
                    if (this.IsBusy)
                    {
                        errorMessage = "Device Busy";
                    }
                }
                return "<node><node-ipv4-router-id>" + this.IPv4_Router_Identifier + "</node-ipv4-router-id><node-name>" + this.Node_Name + "</node-name><error>" + errorMessage + "</error></node>";

            }
        }

        static bool IsValidRPC(XmlDocument xmlDocRequest)
        {
            if (xmlDocRequest.FirstChild.Name == "rpc" && xmlDocRequest.FirstChild.ChildNodes.Count > 0)
            {
                var getRouteInformation = xmlDocRequest.SelectSingleNode("//get-route-information");
                if (getRouteInformation != null)
                {
                    var getRouteInformationDestination = getRouteInformation.SelectSingleNode("//destination");
                    var getRouteInformationExact = getRouteInformation.SelectSingleNode("//exact");

                    if (getRouteInformationDestination != null)
                    {
                        if (getRouteInformationDestination.InnerText.Trim() == "0.0.0.0/0" && getRouteInformationExact == null)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }

                return true;
            }

            return false;
        }

        public Node(string IPv4_Router_Identifier,
                    string NodeUsername,
                    string NodePassword,
                    int NodeKeepAliveInterval,
                    string MessageBrokerURI,
                    string MessageBrokerUsername,
                    string MessageBrokerPassword,
                    string ExchangeName,
                    string RPCRequestQueueSuffix,
                    string RPCRoutingKeyPrefix)
        {
            this.IPv4_Router_Identifier = IPv4_Router_Identifier;
            this.NodeUsername = NodeUsername;
            this.NodePassword = NodePassword;
            this.NodeKeepAliveInterval = NodeKeepAliveInterval;
            this.IsBusy = false;
            this.NetConfClient = new NetConfClient(this.IPv4_Router_Identifier, this.NodeUsername, this.NodePassword);
            this.NetConfClient.KeepAliveInterval = new TimeSpan(0, 0, 30);
            this.NetConfClient.AutomaticMessageIdHandling = false;
            this.ConnectionCheckTimer = new System.Timers.Timer(this.NodeKeepAliveInterval);
            this.ConnectionCheckTimer.Elapsed += ConnectionCheckTimer_Elapsed;
            this.MessageBrokerHost = MessageBrokerURI;
            this.MessageBrokerUsername = MessageBrokerUsername;
            this.MessageBrokerPassword = MessageBrokerPassword;
            this.ExchangeName = ExchangeName;
            this.RPCRequestQueueSuffix = RPCRequestQueueSuffix;
            this.RPCRoutingKeyPrefix = RPCRoutingKeyPrefix;
            this.Retries = 0;
        }

        private void ConnectionCheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!this.IsConnected)
                {
                    Console.WriteLine("Trying to reconnect {0}...", this.IPv4_Router_Identifier);
                    this.Connect();
                }
            }
            catch (Exception ex)
            {
                this.LastExceptionMessage = ex.Message;
                this.Disconnect();
            }
        }
    }
}
