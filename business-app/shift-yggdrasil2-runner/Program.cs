using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using shift.yggdrasil2;

#region JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endregion

#region Grapevine
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Interfaces.Server;
using Grapevine.Shared;
#endregion

#region RestSharp
using RestSharp;
#endregion

namespace shift.yggdrasil2.cicd.runner
{

    class Program
    {

        //Topology
        private static Topology.IGP.Topology igp_topology;
        private static Topology.MPLS.Topology mpls_topology;

        // Config
        static Config config = Config.LoadFromEnvironment(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config_prod.json"));

        static void Main(string[] args)
        {
            #region SHIFT YGGRDASIL2

            igp_topology = new Topology.IGP.Topology();
            mpls_topology = new Topology.MPLS.Topology();

            ConcurrentQueue<JObject> igp_topology_changes_queue = new ConcurrentQueue<JObject>(); // IGP Topology Buffer
            bool igp_topology_task_enabled = false;

            ConcurrentQueue<JObject> mpls_topology_changes_queue = new ConcurrentQueue<JObject>(); // MPLS Topology Buffer
            bool mpls_topology_task_enabled = false;

            // Messaging
            Messaging.ControlQueue cq = new Messaging.ControlQueue(
                    brokerHostname: config.MessageBrokerHostname,
                    brokerPort: config.MessageBrokerPort,
                    brokerUsername: config.MessageBrokerUsername,
                    brokerPassword: config.MessageBrokerPassword,
                    routingKey: config.ControlQueueRoutingKey
                    );

            Messaging.IGP.TopologyQueue tq_igp = new Messaging.IGP.TopologyQueue(
                    brokerHostname: config.MessageBrokerHostname,
                    brokerPort: config.MessageBrokerPort,
                    brokerUsername: config.MessageBrokerUsername,
                    brokerPassword: config.MessageBrokerPassword,
                    routingKey: config.IGPTopologyQueueRoutingKey
                    );
            Messaging.MPLS.TopologyQueue tq_mpls = new Messaging.MPLS.TopologyQueue(
                    brokerHostname: config.MessageBrokerHostname,
                    brokerPort: config.MessageBrokerPort,
                    brokerUsername: config.MessageBrokerUsername,
                    brokerPassword: config.MessageBrokerPassword,
                    routingKey: config.MPLSTopologyQueueRoutingKey
                    );

            Messaging.PerformanceQueue pq = new Messaging.PerformanceQueue(
                    brokerHostname: config.MessageBrokerHostname,
                    brokerPort: config.MessageBrokerPort,
                    brokerUsername: config.MessageBrokerUsername,
                    brokerPassword: config.MessageBrokerPassword,
                    routingKey: config.PerformanceQueueRoutingKey
                    );
            Messaging.NodePCCQueue nq = new Messaging.NodePCCQueue(
                    brokerHostname: config.MessageBrokerHostname,
                    brokerPort: config.MessageBrokerPort,
                    brokerUsername: config.MessageBrokerUsername,
                    brokerPassword: config.MessageBrokerPassword,
                    routingKey: config.NodePCCQueueRoutingKey
                    );

            Messaging.IntentQueueConsumer intentQueueConsumer = new Messaging.IntentQueueConsumer(
                    brokerHostname: config.MessageBrokerHostname,
                    brokerPort: config.MessageBrokerPort,
                    brokerUsername: config.MessageBrokerUsername,
                    brokerPassword: config.MessageBrokerPassword,
                    routingKey: config.IntentQueueRoutingKey
                    );
            #endregion

            #region SHIFT BACKEND CONNECTION

            // IGP Topology Queue Task
            var igp_topology_queue_task = new Task(() =>
            {
                while (igp_topology_task_enabled)
                {
                    while (!igp_topology_changes_queue.IsEmpty)
                    {
                        if (igp_topology_changes_queue.TryDequeue(out JObject data))
                        {
                                // Console.WriteLine("Topology Change: " + data);
                                igp_topology.Update(data);
                        }
                    }

                    Thread.Sleep(10);
                }

            }, TaskCreationOptions.LongRunning);

            // MPLS Topology Queue Task
            var mpls_topology_queue_task = new Task(() =>
            {
                while (mpls_topology_task_enabled)
                {
                    while (!mpls_topology_changes_queue.IsEmpty)
                    {
                        if (mpls_topology_changes_queue.TryDequeue(out JObject data))
                        {
                                // Console.WriteLine("Topology Change: " + data);
                                mpls_topology.Update(data);
                        }
                    }

                    Thread.Sleep(10);
                }

            }, TaskCreationOptions.LongRunning);

            // Control Queue Event Setup
            cq.OnStartCallback += (routing_key) =>
            {
                // IGP Topology Queue
                tq_igp.RoutingKey = routing_key;
                tq_igp.Connect();

                // MPLS Topology Queue
                tq_mpls.Connect();

                // Performance Queue
                pq.Connect();

                // Node PCC Queue
                nq.Connect();

                // Setup Topology Events
                igp_topology.OnNodeUpdateCallback += Igp_topology_OnNodeUpdateCallback;
                igp_topology.OnLinkUpdateCallback += Igp_topology_OnLinkUpdateCallback;
                igp_topology.OnLinkUpCallback += Igp_topology_OnLinkUpCallback;
                igp_topology.OnLinkDownCallback += Igp_topology_OnLinkDownCallback;

                // Load Topology
                data.Neo4J Neo4J = new data.Neo4J
                {
                    Neo4J_URI = config.Neo4J_URI,
                    Neo4J_User = config.Neo4J_User,
                    Neo4J_Password = config.Neo4J_Password
                };

                Neo4J.LoadIGPTopology(igp_topology);
                mpls_topology = Neo4J.LoadMPLSTopology();

                // Start Processing IGP Topology Changes Queue
                igp_topology_task_enabled = true;
                igp_topology_queue_task.Start();

                // Start Processing MPLS Topology Changes Queue
                mpls_topology_task_enabled = true;
                mpls_topology_queue_task.Start();

                // Connect Intent Queue Consumer
                intentQueueConsumer.Connect();
            };

            cq.OnTopologyQueueChangeCallback += (routing_key) =>
            {
                // Disconnect Intent Queue Cosumer
                intentQueueConsumer.Disconnect();

                // Stop Processing Topology Changes Queue
                igp_topology_task_enabled = false;
                while (igp_topology_queue_task.Status == TaskStatus.Running)
                {
                    Thread.Sleep(100);
                }

                // IGP Topology Queue
                tq_igp.RoutingKey = routing_key;
                tq_igp.Connect();

                // Setup Topology Events
                igp_topology.OnNodeUpdateCallback += Igp_topology_OnNodeUpdateCallback;
                igp_topology.OnLinkUpdateCallback += Igp_topology_OnLinkUpdateCallback;
                igp_topology.OnLinkUpCallback += Igp_topology_OnLinkUpCallback;
                igp_topology.OnLinkDownCallback += Igp_topology_OnLinkDownCallback;

                // Load Topology
                data.Neo4J Neo4J = new data.Neo4J
                {
                    Neo4J_URI = config.Neo4J_URI,
                    Neo4J_User = config.Neo4J_User,
                    Neo4J_Password = config.Neo4J_Password
                };

                Neo4J.LoadIGPTopology(igp_topology);
                mpls_topology = Neo4J.LoadMPLSTopology();

                // Start Processing Topology Changes Queue
                igp_topology_task_enabled = true;
                igp_topology_queue_task.Start();

                // Connect Intent Queue Consumer
                intentQueueConsumer.Connect();
            };

            // IGP Topology Queue Event Setup
            tq_igp.OnTopologyChangeCallback += (data) =>
            {
                    // Enqueue Topology Change
                    igp_topology_changes_queue.Enqueue(data);
            };

            // MPLS Topology Queue Event Setup
            tq_mpls.OnTopologyChangeCallback += (data) =>
            {
                    // Enqueue Topology Change
                    mpls_topology_changes_queue.Enqueue(data);
            };

            // Performance Queue Event Setup
            pq.OnPerformanceUpdateCallback += (data) =>
            {
                    //Console.WriteLine("Performance Update: " + data);

                    igp_topology.Update(data);
            };

            // Node PCC Queue Event Setup
            nq.OnNodePCCUpdateCallback += (data) =>
            {
                    // Console.WriteLine("Node PCC Update: " + data);

                    igp_topology.Update(data);
            };

            // Intent Queue Event Setup
            intentQueueConsumer.OnIntentJobCallback += IntentQueueConsumer_OnIntentJobCallback;

            // Control Queue Connect
            cq.Connect();

            #endregion

            Console.WriteLine("Runner Started.");

            string cmd = Console.ReadLine();

            while (cmd != "exit")
            {
                Console.WriteLine("Command not found.");
                Console.Write("> ");
                cmd = Console.ReadLine();
            }

            #region SHIFT BACKEND DISCONNECT
            cq.Disconnect();

            tq_igp.Disconnect();
            tq_mpls.Disconnect();

            pq.Disconnect();
            nq.Disconnect();

            intentQueueConsumer.Disconnect();
            #endregion

        }

        private static void IntentQueueConsumer_OnIntentJobCallback(JObject data)
        {
            try
            {
                var code = Base64Decode(data["Base64IntentCode"].Value<string>());

                var compilerResults = Intent.IntentCompiler.CompileCSharpString(code);

                var tempType = compilerResults.CompiledAssembly.GetType("ShiftPolicy");

                #region Mandatory Properties
                bool enabled = (bool)tempType.GetProperty("Enabled").GetGetMethod().Invoke(null, null);
                int period = (int)tempType.GetProperty("Period").GetGetMethod().Invoke(null, null);
                DateTime validBefore = (DateTime)tempType.GetProperty("ValidBefore").GetGetMethod().Invoke(null, null);
                #endregion

                // Iterate Node

                Topology.Node.Node iterateNode = new Topology.Node.Node();

                try
                {
                    iterateNode = Topology.Node.Node.FromJson(data["IterateNode"].Value<JToken>().ToString());
                }
                catch (Exception)
                {
                    iterateNode = null;
                }
                
                //

                if (enabled && validBefore > DateTime.Now)
                {
                    object[] parameters = { igp_topology, mpls_topology, new PathComputation.PathComputation.YggdrasilNM2(), iterateNode };

                    var intentResult = tempType.GetMethod("Intent").Invoke(null, parameters);

                    Console.WriteLine("Response Type: {0}", intentResult.GetType());

                    OutputProcessor.IntentProcessor(intentResult, mpls_topology, igp_topology,
                        config.SDNControllerURI, config.SDNControllerUsername, config.SDNControllerPassword,
                        config.AnsibleTowerURI, config.AnsibleTowerUsername, config.AnsibleTowerPassword);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        #region Topology Callbacks

        private static void Igp_topology_OnNodeUpdateCallback(yggdrasil2.Topology.Node.Node node)
        {
        }

        private static void Igp_topology_OnLinkUpdateCallback(yggdrasil2.Topology.IGP.Link.Link link)
        {
        }


        private static void Igp_topology_OnLinkUpCallback(yggdrasil2.Topology.IGP.Link.Link link)
        {
        }

        private static void Igp_topology_OnLinkDownCallback(yggdrasil2.Topology.IGP.Link.Link link)
        {
        }

        #endregion
    }
}