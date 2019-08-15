using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Diagnostics;

#region JSON.NET
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using J = Newtonsoft.Json.JsonPropertyAttribute;
#endregion

#region LibGit2Sharp
using LibGit2Sharp;
#endregion

#region RestSharp
using RestSharp;
#endregion

#region Prometheus
using Prometheus;
#endregion

namespace shift.yggdrasil2.cicd.scheduler
{
    [Serializable]
    class CICDJob
    {
        [J("Id")] public Guid Id { get; private set; }
        [J("JobType")] [JsonConverter(typeof(StringEnumConverter))] public JobType JobType { get; set; }
        [J("RepositoryURL")] public string RepositoryURL { get; private set; }
        [J("DeployUsername")] public string DeployUsername { get; private set; }
        [J("DeployPassword")] public string DeployPassword { get; private set; }
        [J("Started")] public bool Started { get; private set; }
        [J("Completed")] public bool Completed { get; private set; }
        [J("OverallSuccess")] public bool OverallSuccess { get; private set; }
        [J("LastExceptionMessage")] public string LastExceptionMessage { get; private set; }
        [J("ResultList")] public List<CICDJobResult> JobResults { get; set; }

        [JsonIgnore] public Topology.IGP.Topology igp_topology { get; set; }
        [JsonIgnore] public Topology.MPLS.Topology mpls_topology { get; set; }

        [JsonIgnore] public BackgroundWorker Worker { get; private set; }


        public delegate void OnJobCompletedHandler(CICDJob cicdJob);
        public event OnJobCompletedHandler OnJobCompletedCallback;

        #region Metrics
        private static readonly Histogram BuildDuration = Metrics
            .CreateHistogram(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_BuildDuration",
            "Histogram of Build processing durations.");

        private static readonly Histogram TestDuration = Metrics
            .CreateHistogram(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_TestDuration",
            "Histogram of Test processing durations.");

        private static readonly Histogram DeployDuration = Metrics
                    .CreateHistogram(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_DeployDuration",
                    "Histogram of Deploy processing durations.");

        private static readonly Counter SuccessfulBuildCount = Metrics
                    .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_SuccessfulBuildCount",
                    "Number of successful Build jobs.");

        private static readonly Counter SuccessfulTestCount = Metrics
                    .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_SuccessfulTestCount",
                    "Number of successful Test jobs.");

        private static readonly Counter SuccessfulDeployCount = Metrics
                    .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_SuccessfulDeployCount",
                    "Number of successful Deploy jobs.");

        private static readonly Counter FailedBuildCount = Metrics
                    .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_FailedBuildCount",
                    "Number of failed Build jobs.");

        private static readonly Counter FailedTestCount = Metrics
                    .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_FailedTestCount",
                    "Number of failed Test jobs.");

        private static readonly Counter FailedDeployCount = Metrics
                    .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_FailedDeployCount",
                    "Number of failed Deploy jobs.");

        private static readonly Counter FailedRepositoryCount = Metrics
                    .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_FailedRepositoryCount",
                    "Number of failed Repository operations.");

        private static readonly Counter JobExceptionCount = Metrics
                            .CreateCounter(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace.Replace(".", "_") + "_JobExceptionCount",
                            "Number of aggregated exceptions thrown while processing CI/CD jobs.");

        #endregion

        public CICDJob()
        {

        }

        public CICDJob(string repo_url, string user, string password, JobType jobType)
        {
            this.Completed = false;
            this.Id = Guid.NewGuid();
            this.RepositoryURL = repo_url;
            this.DeployUsername = user;
            this.DeployPassword = password;
            this.JobType = jobType;

            this.JobResults = new List<CICDJobResult>();

            this.Worker = new BackgroundWorker();
            this.Worker.DoWork += Worker_DoWork;
            this.Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        public CICDJob(string repo_url, string user, string password, JobType jobType,
            Topology.IGP.Topology igp_topology, Topology.MPLS.Topology mpls_topology)
        {
            this.Completed = false;
            this.Id = Guid.NewGuid();
            this.RepositoryURL = repo_url;
            this.DeployUsername = user;
            this.DeployPassword = password;
            this.JobType = jobType;

            this.igp_topology = igp_topology;
            this.mpls_topology = mpls_topology;

            this.JobResults = new List<CICDJobResult>();

            this.Worker = new BackgroundWorker();
            this.Worker.DoWork += Worker_DoWork;
            this.Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {

            bool overall_success = true;

            this.Started = true;

            var repo_url = (string)e.Argument;

            try
            {
                Pull(repo_url, this.DeployUsername, this.DeployPassword);

                var local_repo = GetRepoLocalDirectory(repo_url);
                var files = GetFileList(new DirectoryInfo(local_repo));

                foreach (var file in files)
                {
                    switch (this.JobType)
                    {
                        case JobType.Build:
                            using (BuildDuration.NewTimer())
                            {
                                try
                                {
                                    var compilerResults = Intent.IntentCompiler.CompileCSharpString(File.ReadAllText(file));

                                    var jobResult = new CICDJobResult(compilerResults, JobType.Build, file);

                                    if (!jobResult.Success) { overall_success = false; FailedBuildCount.Inc(); } else { SuccessfulBuildCount.Inc(); }

                                    this.JobResults.Add(jobResult);
                                }
                                catch (Exception ex)
                                {
                                    overall_success = false;
                                    this.LastExceptionMessage = ex.Message;
                                    Console.WriteLine(ex.Message);

                                    JobExceptionCount.Inc();
                                } 
                            }
                            break;
                        case JobType.Test:
                            using (TestDuration.NewTimer())
                            {
                                try
                                {
                                    var compilerResults = Intent.IntentCompiler.CompileCSharpString(File.ReadAllText(file));

                                    var jobResult = new CICDJobResult(compilerResults, JobType.Test, this.igp_topology, this.mpls_topology, file);

                                    if (!jobResult.Success) { overall_success = false; FailedTestCount.Inc(); } else { SuccessfulTestCount.Inc(); }

                                    this.JobResults.Add(jobResult);
                                }
                                catch (Exception ex)
                                {
                                    overall_success = false;
                                    this.LastExceptionMessage = ex.Message;
                                    Console.WriteLine(ex.Message);

                                    JobExceptionCount.Inc();
                                }
                                break; 
                            }
                        case JobType.Deploy:
                            using (DeployDuration.NewTimer())
                            {
                                try
                                {
                                    var compilerResults = Intent.IntentCompiler.CompileCSharpString(File.ReadAllText(file));

                                    var jobResult = new CICDJobResult(compilerResults, JobType.Deploy, this.igp_topology, file);

                                    if (!jobResult.Success) { overall_success = false; FailedDeployCount.Inc(); } else { SuccessfulDeployCount.Inc(); }

                                    this.JobResults.Add(jobResult);
                                }
                                catch (Exception ex)
                                {
                                    overall_success = false;
                                    this.LastExceptionMessage = ex.Message;
                                    Console.WriteLine(ex.Message);

                                    JobExceptionCount.Inc();
                                } 
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                overall_success = false;
                this.LastExceptionMessage = ex.Message;
                Console.WriteLine(ex.Message);

                JobExceptionCount.Inc();
            }

            this.OverallSuccess = overall_success;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Completed = true;
            this.OnJobCompletedCallback?.Invoke(this);
        }

        public void Start()
        {
            if (!this.Started)
            {
                this.Worker.RunWorkerAsync(this.RepositoryURL);
            }
        }

        private static List<string> GetFileList(DirectoryInfo dir_info)
        {
            List<string> files = new List<string>();

            // Hide directories and files starting with '.'
            if (!dir_info.Name.StartsWith("."))
            {
                // Add subdirectories.
                foreach (DirectoryInfo subdir in dir_info.GetDirectories())
                {
                    files.AddRange(GetFileList(subdir));
                }

                // Add files
                foreach (FileInfo file_info in dir_info.GetFiles())
                {
                    if (file_info.Name.EndsWith(".shift"))
                    {
                        files.Add(file_info.FullName);
                    }
                }
            }

            return files;
        }

        private static string GetRepoLocalDirectory(string repo_url)
        {
            string[] r = repo_url.Split("@".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var repo_path = r[r.Length - 1];

            string app_path = "";

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                app_path = Path.Combine("/var", "shift", "scheduler");

                return Path.Combine(app_path, repo_path.TrimEnd(".git".ToCharArray()));
            }
            else
            {
                app_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "shift", "scheduler");

                return Path.Combine(app_path, repo_path.Replace('/', '\\'));
            }
        }

        private static void Pull(string repo_url, string user, string password)
        {
            var repo_full_path = GetRepoLocalDirectory(repo_url);


            if (!Directory.Exists(repo_full_path))
            {
                Directory.CreateDirectory(repo_full_path);
            }

            // Git Repo

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var client = new RestClient("http://localhost:6666");
                var request = new RestRequest("repository/update", Method.POST);

                request.AddJsonBody(new { repository_url = repo_url });

                IRestResponse response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    FailedRepositoryCount.Inc();
                    JobExceptionCount.Inc();
                    throw new Exception("ERROR: UNABLE TO UPDATE REPOSITORY.");
                }

            }
            else
            {
                if (Repository.IsValid(repo_full_path))
                {
                    using (var repo = new Repository(repo_full_path))
                    {

                        var po = new PullOptions();
                        po.FetchOptions = new FetchOptions();

                        po.FetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                        {
                            return new UsernamePasswordCredentials()
                            {
                                Username = user,
                                Password = password
                            };
                        };

                        Signature signature = new Signature("SHIFT Scheduler", "SHIFT Scheduler", DateTime.Now);

                        Commands.Pull(repo, signature, po);

                    }
                }
                else
                {
                    var co = new CloneOptions();

                    co.CredentialsProvider = (_url, _user, _cred) =>
                    {
                        return new UsernamePasswordCredentials()
                        {
                            Username = user,
                            Password = password
                        };
                    };

                    Repository.Clone(repo_url, repo_full_path, co);
                }
            }

        }
    }
    
    enum JobType
    {
        Build = 0,
        Test= 1,
        Deploy =2
    }
}
