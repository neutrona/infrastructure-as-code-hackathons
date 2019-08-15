using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

using System.Reflection;


using RestSharp;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#region Prometheus
using Prometheus;
#endregion

namespace shift.yggdrasil2.cicd.scheduler
{
    class Scheduler
    {
        private static ConcurrentDictionary<Guid, CICDJob> cicd_jobs = new ConcurrentDictionary<Guid, CICDJob>();
        private static ConcurrentDictionary<string, IntentJob> intent_jobs = new ConcurrentDictionary<string, IntentJob>();

        public Topology.IGP.Topology igp_topology { get; private set; }

        public Config config { get; private set; }

        private static Timer IntentTimer = new Timer(1000);

        // private static Timer IntentTimer = new Timer(1000*60);

        public delegate void OnIntentDueHandler(IntentJob intentJob);
        public event OnIntentDueHandler OnIntentDueCallback;


        #region Metrics

        private static readonly Gauge IntentJobs = Metrics
            .CreateGauge(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_IntentJobs",
            "Number of Intent jobs scheduled for processing.");

        #endregion

        public static CICDJob GetCICDJob(Guid jobId)
        {
            return cicd_jobs[jobId];
        }

        public static CICDJob RemoveCICDJob(Guid jobId)
        {
            var r = new CICDJob();
            cicd_jobs.TryRemove(jobId, out r);

            return r;
        }

        public static List<CICDJob> GetCICDJobs()
        {
            return cicd_jobs.Values.ToList();
        }

        public static List<IntentJob> GetIntentJobs(string match_on)
        {
            if (!string.IsNullOrWhiteSpace(match_on))
            {
                return intent_jobs.Values.Where(j => Regex.IsMatch(j.FileName, match_on)).ToList();

            } else
            {
                return intent_jobs.Values.ToList();
            }
        }

        public static CICDJob Build(string repo_url, string user, string password)
        {
            CICDJob newJob = new CICDJob(repo_url, user, password, JobType.Build);

            cicd_jobs.TryAdd(newJob.Id, newJob);

            return newJob;
        }

        public static CICDJob Test(string repo_url, string user, string password, Topology.IGP.Topology igp_topology, Topology.MPLS.Topology mpls_topology)
        {
            CICDJob newJob = new CICDJob(repo_url, user, password, JobType.Test, igp_topology, mpls_topology);

            cicd_jobs.TryAdd(newJob.Id, newJob);

            return newJob;
        }

        public static CICDJob Deploy(string repo_url, string user, string password, Topology.IGP.Topology igp_topology, Topology.MPLS.Topology mpls_topology)
        {
            CICDJob newJob = new CICDJob(repo_url, user, password, JobType.Deploy, igp_topology, mpls_topology);

            // Update intent jobs concurrent dict
            newJob.OnJobCompletedCallback += (CICDJob cicdJob) => {
                foreach (var result in cicdJob.JobResults)
                {
                    if (result.IntentJob != null)
                    {
                        intent_jobs.AddOrUpdate(result.IntentJob.FileName, result.IntentJob, (k, v) => result.IntentJob);

                        IntentJobs.Set(intent_jobs.Count);
                    }
                }
            };

            cicd_jobs.TryAdd(newJob.Id, newJob);

            return newJob;
        }

        public Scheduler(Topology.IGP.Topology igpTopology, Config config)
        {
            this.igp_topology = igpTopology;
            this.config = config;

            IntentTimer.Elapsed += IntentTimer_Elapsed;
            IntentTimer.Enabled = true;
        }

        private void IntentTimer_Elapsed(object sender, ElapsedEventArgs e)
        {

            // k8s scaling

            try
            {

                var client = new RestClient(this.config.K8sAPIHelperURI);
                
                var getReplicas = new RestRequest("/statefulsets/replicas/" + this.config.K8sNamespace + "/" + this.config.K8sYggdrasil2RunnerStatefulSetName, Method.GET);
 
                var response = client.Execute(getReplicas);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JObject jObject = JObject.Parse(response.Content);

                    var replicas = (int)jObject["Replicas"];

                    // Console.WriteLine("k8s :::: {0} has {1} replicas.", jObject["Name"], replicas);

                    // Replicas Policy
                    var nodes = this.igp_topology.Nodes.Count(n => !string.IsNullOrWhiteSpace(n.IPv4RouterIdentifier));

                    int iterativeJobs = intent_jobs.Count(j => j.Value.RequiresIteration == true);
                    int nonIterativeJobs = intent_jobs.Count - iterativeJobs;

                    int desiredReplicas = (int)(Math.Ceiling(nodes / 2.0) * Math.Ceiling(iterativeJobs / 2.0)) +
                        (int)(Math.Ceiling(nodes / 2.0) * Math.Ceiling(nonIterativeJobs / 8.0));
                    //
                    
                    if (replicas != desiredReplicas && desiredReplicas > 0)
                    {
                        var setReplicas = new RestRequest("/statefulsets/replicas/" + this.config.K8sNamespace + "/" + this.config.K8sYggdrasil2RunnerStatefulSetName + "/update/" + desiredReplicas, Method.POST);

                        response = client.Execute(setReplicas);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            Console.WriteLine("k8s :::: Scaled {0} to {1} replicas.", jObject["Name"], desiredReplicas);
                        }
                        else
                        {
                            Console.WriteLine("k8s :::: Unable to scale {0} to {1} replicas.", jObject["Name"], desiredReplicas);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("k8s :::: Unable to get replicas for {0}.", config.K8sYggdrasil2RunnerStatefulSetName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} ::: {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            foreach (var file_name in intent_jobs.Keys)
            {
                if (File.Exists(file_name) && DateTime.Now < intent_jobs[file_name].ValidBefore)
                {
                    // If the file exists then check next intent run

                    var next_run = intent_jobs[file_name].NextRun();

                    // If intent is due then raise intent job event
                    if(DateTime.Now >= next_run)
                    {
                        this.OnIntentDueCallback?.Invoke(intent_jobs[file_name]);
                    }

                } else
                {
                    // Remove job if the intent file does not exist anymore
                    // or the intent has expired
                    var value = new IntentJob();
                    intent_jobs.TryRemove(file_name, out value);

                    IntentJobs.Set(intent_jobs.Count);
                }
            }
        }
    }
}
