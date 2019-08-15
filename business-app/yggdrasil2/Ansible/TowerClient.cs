using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RestSharp;
using RestSharp.Authenticators;
using System.Net;

namespace shift.yggdrasil2.Ansible
{
    public class TowerClient
    {
        private Uri TowerURI { get; set; }
        private string TowerUser { get; set; }
        private string TowerPassword { get; set; }

        public int LaunchJob(int templateId, object extraVars, string limit)
        {
            try
            {
                AnsibleTowerLaunchPayload payload = new AnsibleTowerLaunchPayload()
                {
                    extra_vars = extraVars,
                    limit = limit
                };

                var jsonVars = JsonConvert.SerializeObject(payload);

                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;

                var client = new RestClient(this.TowerURI);
                var authenticator = new HttpBasicAuthenticator(this.TowerUser, this.TowerPassword);
                
                var request = new RestRequest("job_templates/" + templateId + "/launch/", Method.POST);

                request.AddParameter("application/json", jsonVars, ParameterType.RequestBody);

                authenticator.Authenticate(client, request);

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var content = JObject.Parse(response.Content);
                    var jobId = content.Value<int>("id");

                    return jobId;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }

        }

        public bool CheckJobCompleted(int jobId)
        {
            try
            {
                var client = new RestClient(this.TowerURI);
                client.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                var authenticator = new HttpBasicAuthenticator(this.TowerUser, this.TowerPassword);

                var request = new RestRequest("jobs/" + jobId + "/", Method.GET);

                authenticator.Authenticate(client, request);

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = JObject.Parse(response.Content);
                    var jobStatus = content.Value<string>("status");

                    if (new string[] {"pending", "waiting", "running" }.Contains(jobStatus))
                    {
                        return false;
                    } else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool CheckJobSuccessful(int jobId)
        {
            try
            {
                var client = new RestClient(this.TowerURI);
                client.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                var authenticator = new HttpBasicAuthenticator(this.TowerUser, this.TowerPassword);

                var request = new RestRequest("jobs/" + jobId + "/", Method.GET);

                authenticator.Authenticate(client, request);

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = JObject.Parse(response.Content);
                    return !content.Value<bool>("failed");
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public TowerClient(string towerURI, string towerUser, string towerPassword)
        {
            this.TowerURI = new Uri(towerURI);
            this.TowerUser = towerUser;
            this.TowerPassword = towerPassword;
        }

        class AnsibleTowerLaunchPayload
        {
            public object extra_vars { get; set; }
            public string limit { get; set; }
        }
    }
}
