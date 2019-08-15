using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

using RestSharp;
using RestSharp.Authenticators;

using Stubble.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace shift.yggdrasil2.Controllers.OpenDayLight.Nitrogen
{
    public class ODLClient
    {
        public Uri ControllerURI { get; set; }
        public string ControllerUsername { get; set; }
        public string ControllerPassword { get; set; }

        public bool UpdateLabelSwitchedPath(Topology.MPLS.LabelSwitchedPath.LabelSwitchedPath LabelSwitchedPath)
        {
            // Fill PCC property if empty
            if (string.IsNullOrWhiteSpace(LabelSwitchedPath.PCC)) { LabelSwitchedPath.PCC = LabelSwitchedPath.IPv4TunnelSenderAddress; }

            // Parse the template:
            var contents = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Controller", "OpenDayLight", "Nitrogen", "Templates", "pcep-update-lsp-objects.odlt"));

            // Combine the model with the template to get content:
            var content = StaticStubbleRenderer.Render(contents, LabelSwitchedPath);


            var client = new RestClient();
            client.BaseUrl = this.ControllerURI;
            client.Authenticator = new HttpBasicAuthenticator(this.ControllerUsername, this.ControllerPassword);

            var request = new RestRequest(Method.POST);
            request.AddParameter("application/xml", content, ParameterType.RequestBody);
            request.Resource = "restconf/operations/network-topology-pcep:update-lsp";

            IRestResponse response = client.Execute(request);



            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = JObject.Parse(response.Content);

                JToken output;
                if (result.TryGetValue("output", out output))
                {
                    if (!output.HasValues)
                    {

                        Console.WriteLine("SUCCESS: PCC {0} --> Updated LSP {1}/{2}", LabelSwitchedPath.PCC, LabelSwitchedPath.ParentId, LabelSwitchedPath.SymbolicPathName);

                        return true;
                    }
                    else
                    {
                        if (output["failure"] != null)
                        {
                            Console.WriteLine("FAILURE: PCC {0} --> {1}", LabelSwitchedPath.PCC, output["failure"].ToString());

                            if (output["error"] != null)
                            {
                                Console.WriteLine("ERROR: PCC {0} --> {1}", LabelSwitchedPath.PCC, output["error"].ToString());
                            }

                            return false;
                        }
                        else
                        {
                            Console.WriteLine("FAILURE: PCC {0} --> UNKNOWN FAILURE", LabelSwitchedPath.PCC);

                            return false;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("FAILURE: PCC {0} --> UNKNOWN FAILURE", LabelSwitchedPath.PCC);

                    return false;
                }
            }
            else
            {
                Console.WriteLine("FAILURE: PCC {0} --> ODL: REST FAILURE", LabelSwitchedPath.PCC);

                return false;
            }
        }

        public ODLClient(string ControllerURI, string Username, string password)
        {
            this.ControllerURI = new Uri(ControllerURI);
            this.ControllerUsername = Username;
            this.ControllerPassword = password;
        }
    }
}
