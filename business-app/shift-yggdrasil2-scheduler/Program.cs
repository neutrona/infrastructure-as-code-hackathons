using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Reflection;

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

#region Prometheus
using Prometheus;
#endregion

namespace shift.yggdrasil2.cicd.scheduler
{
    class Program
    {
        // Scheduler
        public static Scheduler Scheduler { get; private set; }

        // Messaging - Intent Queue
        private static Messaging.IntentQueueProducer IntentQueueProducer { get; set; }

        // Config
        static Config config = Config.LoadFromEnvironment(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config_prod.json"));

        // Topology
        private static Topology.IGP.Topology igp_topology;
        private static Topology.MPLS.Topology mpls_topology;

        #region Metrics
        private static readonly Gauge CICDJobs = Metrics
            .CreateGauge(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_CICDJobs",
            "Number of CI/CD jobs waiting for processing.");

        private static readonly Counter IntentJobTasksCount = Metrics
            .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_IntentJobTasks",
            "Number of processed Intent job tasks delivered for execution.");

        private static readonly Gauge IGPNodes = Metrics
            .CreateGauge(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_IGPTopologyNodes",
            "Number of IGP topology nodes.");

        private static readonly Gauge IGPLinks = Metrics
            .CreateGauge(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_IGPTopologyLinks",
            "Number of IGP topology links.");

        private static readonly Gauge MPLSHierarchicalLabelSwitchedPaths = Metrics
            .CreateGauge(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_MPLSHierarchicalLabelSwitchedPaths",
            "Number of MPLS Hierarchical Label Switched Paths.");

        #endregion

        static void Main(string[] args)
        {
            using (var server = new RestServer())
            {
                #region Metrics Server
                var metricServer = new MetricServer(port: 60000);
                metricServer.Start();
                #endregion

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

                IntentQueueProducer = new Messaging.IntentQueueProducer(
                    brokerHostname: config.MessageBrokerHostname,
                    brokerPort: config.MessageBrokerPort,
                    brokerUsername: config.MessageBrokerUsername,
                    brokerPassword: config.MessageBrokerPassword,
                    routingKey: config.IntentQueueRoutingKey
                    );
                #endregion

                #region SHIFT BACKEND CONNECTION

                // Messaging

                // Intent Queue

                IntentQueueProducer.Connect();
                                             
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
                                MPLSHierarchicalLabelSwitchedPaths.Set(mpls_topology.HierarchicalLabelSwitchedPaths.Count);
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

                    MPLSHierarchicalLabelSwitchedPaths.Set(mpls_topology.HierarchicalLabelSwitchedPaths.Count);


                    // Start Processing IGP Topology Changes Queue
                    igp_topology_task_enabled = true;
                    igp_topology_queue_task.Start();

                    // Start Processing MPLS Topology Changes Queue
                    mpls_topology_task_enabled = true;
                    mpls_topology_queue_task.Start();
                };

                cq.OnTopologyQueueChangeCallback += (routing_key) =>
                {

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

                    MPLSHierarchicalLabelSwitchedPaths.Set(mpls_topology.HierarchicalLabelSwitchedPaths.Count);


                    // Start Processing Topology Changes Queue
                    igp_topology_task_enabled = true;
                    igp_topology_queue_task.Start();
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
                    // Console.WriteLine("Performance Update: " + data);

                    igp_topology.Update(data);
                };

                // Node PCC Queue Event Setup
                nq.OnNodePCCUpdateCallback += (data) =>
                {
                    // Console.WriteLine("Node PCC Update: " + data);

                    igp_topology.Update(data);
                };

                // Control Queue Connect
                cq.Connect();

                #endregion

                #region Scheduler
                Scheduler = new Scheduler(igp_topology, config);
                Scheduler.OnIntentDueCallback += Scheduler_OnIntentDueCallback;
                #endregion

                #region REST SERVER

                server.Host = "+";
                server.Port = "8375";
                server.LogToConsole().Start();

                Console.WriteLine("Server Started.");

                string cmd = Console.ReadLine();

                while (cmd != "exit")
                {
                    Console.WriteLine("Command not found.");
                    Console.Write("> ");
                    cmd = Console.ReadLine();
                }

                server.Stop();

                #endregion

                #region SHIFT BACKEND DISCONNECT
                IntentQueueProducer.Disconnect();

                cq.Disconnect();

                tq_igp.Disconnect();
                tq_mpls.Disconnect();

                pq.Disconnect();
                nq.Disconnect();
                #endregion
            }
        }

        private static void Scheduler_OnIntentDueCallback(IntentJob intentJob)
        {
            if (intentJob.RequiresIteration)
            {
                try
                {
                    var compilerResults = Intent.IntentCompiler.CompileCSharpString(File.ReadAllText(intentJob.FileName));

                    var tempType = compilerResults.CompiledAssembly.GetType("ShiftPolicy");

                    List<Topology.Node.Node> iterateNodes = (List<Topology.Node.Node>)tempType.GetMethod("IterateNodes").Invoke(null, new object[] { igp_topology });

                    foreach (var node in iterateNodes)
                    {
                        var iJob = new IntentJob(
                            fileName: intentJob.FileName,
                            intentCode: IntentJob.Base64Decode(intentJob.Base64IntentCode),
                            period: intentJob.Period,
                            validBefore: intentJob.ValidBefore,
                            requiresIteration: intentJob.RequiresIteration,
                            iterateNode: node
                            );

                        IntentQueueProducer.Publish(JsonConvert.SerializeObject(iJob));
                        IntentJobTasksCount.Inc();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                IntentQueueProducer.Publish(JsonConvert.SerializeObject(intentJob));
                IntentJobTasksCount.Inc();
            }
        }

        [RestResource]
        public class APIv1
        {
            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "/api/v1/repository/build")]
            public IHttpContext RepositoryBuild(IHttpContext context)
            {
                try
                {
                    var payload = JObject.Parse(context.Request.Payload);

                    var repo_url = payload["repository_url"].Value<string>();
                    var commit = payload["commit"].Value<string>();
                    var deploy_user = payload["deploy_user"].Value<string>();
                    var deploy_password = payload["deploy_password"].Value<string>();


                    var job = Scheduler.Build(repo_url, deploy_user, deploy_password);
                    job.Start();

                    CICDJobs.Inc();

                    context.Response.StatusCode = HttpStatusCode.Ok;
                    context.Response.ContentType = ContentType.JSON;
                    context.Response.SendResponse(JsonConvert.SerializeObject(job));
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = HttpStatusCode.InternalServerError;
                    context.Response.SendResponse(ex.Message);
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "/api/v1/repository/test")]
            public IHttpContext RepositoryTest(IHttpContext context)
            {
                try
                {
                    var payload = JObject.Parse(context.Request.Payload);

                    var repo_url = payload["repository_url"].Value<string>();
                    var commit = payload["commit"].Value<string>();
                    var deploy_user = payload["deploy_user"].Value<string>();
                    var deploy_password = payload["deploy_password"].Value<string>();


                    var job = Scheduler.Test(repo_url, deploy_user, deploy_password, igp_topology, mpls_topology);
                    job.Start();
                    CICDJobs.Inc();

                    context.Response.StatusCode = HttpStatusCode.Ok;
                    context.Response.ContentType = ContentType.JSON;
                    context.Response.SendResponse(JsonConvert.SerializeObject(job));
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = HttpStatusCode.InternalServerError;
                    context.Response.SendResponse(ex.Message);
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.POST, PathInfo = "/api/v1/repository/deploy")]
            public IHttpContext RepositoryDeploy(IHttpContext context)
            {
                try
                {
                    var payload = JObject.Parse(context.Request.Payload);

                    var repo_url = payload["repository_url"].Value<string>();
                    var commit = payload["commit"].Value<string>();
                    var deploy_user = payload["deploy_user"].Value<string>();
                    var deploy_password = payload["deploy_password"].Value<string>();


                    var job = Scheduler.Deploy(repo_url, deploy_user, deploy_password, igp_topology, mpls_topology);
                    job.Start();
                    CICDJobs.Inc();

                    context.Response.StatusCode = HttpStatusCode.Ok;
                    context.Response.ContentType = ContentType.JSON;
                    context.Response.SendResponse(JsonConvert.SerializeObject(job));
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = HttpStatusCode.InternalServerError;
                    context.Response.SendResponse(ex.Message);
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "/api/v1/repository/jobs")]
            public IHttpContext GetRepositoryJobs(IHttpContext context)
            {
                try
                {
                    try
                    {
                        var jobId = context.Request.QueryString["id"];

                        if (jobId != null)
                        {
                            var job = Scheduler.GetCICDJob(new Guid(jobId));

                            context.Response.StatusCode = HttpStatusCode.Ok;
                            context.Response.ContentType = ContentType.JSON;
                            context.Response.SendResponse(JsonConvert.SerializeObject(job));

                            if (job.Completed)
                            {
                                Scheduler.RemoveCICDJob(job.Id);
                                CICDJobs.Dec();
                            }
                        }
                        else
                        {
                            var jobs = Scheduler.GetCICDJobs();

                            context.Response.StatusCode = HttpStatusCode.Ok;
                            context.Response.ContentType = ContentType.JSON;
                            context.Response.SendResponse(JsonConvert.SerializeObject(jobs));
                        }
                    }
                    catch (Exception)
                    {
                        context.Response.StatusCode = HttpStatusCode.NotFound;
                        context.Response.SendResponse(string.Empty);
                    }

                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = HttpStatusCode.InternalServerError;
                    context.Response.SendResponse(ex.Message);
                }
                return context;
            }

            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "/api/v1/intent/jobs")]
            public IHttpContext GetIntentJobs(IHttpContext context)
            {
                try
                {
                    try
                    {
                        var match_on = context.Request.QueryString["match"];

                        var jobs = Scheduler.GetIntentJobs(match_on);

                        context.Response.StatusCode = HttpStatusCode.Ok;
                        context.Response.ContentType = ContentType.JSON;
                        context.Response.SendResponse(JsonConvert.SerializeObject(jobs));
                    }
                    catch (Exception)
                    {
                        context.Response.StatusCode = HttpStatusCode.NotFound;
                        context.Response.SendResponse(string.Empty);
                    }

                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = HttpStatusCode.InternalServerError;
                    context.Response.SendResponse(ex.Message);
                }
                return context;
            }
        }

        #region Topology Callbacks

        private static void Igp_topology_OnNodeUpdateCallback(yggdrasil2.Topology.Node.Node node)
        {
            IGPNodes.Set(igp_topology.Nodes.Count(n => n.IsPseudonode == false));
        }

        private static void Igp_topology_OnLinkUpdateCallback(yggdrasil2.Topology.IGP.Link.Link link)
        {
            IGPLinks.Set(igp_topology.Links.Count);
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
