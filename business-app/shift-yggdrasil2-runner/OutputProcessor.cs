using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using shift.yggdrasil2.Topology.MPLS;
using shift.yggdrasil2.Topology.IGP;


namespace shift.yggdrasil2.cicd.runner
{
    class OutputProcessor
    {
        private static void HLSP_Processor(
            Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath hlsp,
            Topology.IGP.Topology igp_topology, Topology.MPLS.Topology mpls_topology,
            string controller_uri, string controller_username, string controller_password,
            string ansible_tower_uri, string ansible_tower_username, string ansible_tower_password)
        {
            if (string.IsNullOrWhiteSpace(hlsp.PCC)) { hlsp.PCC = hlsp.IPv4TunnelSenderAddress; }
            if (string.IsNullOrWhiteSpace(hlsp.AnsibleTowerHost)) { hlsp.AnsibleTowerHost = hlsp.PCC; }

            // Check if HLSP needs to be removed from configuration
            if (!hlsp.Delete)
            {
                // SDN Controller API Client
                Controllers.OpenDayLight.Nitrogen.ODLClient nitrogen = new Controllers.OpenDayLight.Nitrogen.ODLClient(controller_uri, controller_username, controller_password);


                // Check if HLSP does not exist or is marked as requiring a config update and launch Ansible Tower job to deploy (or update) base HLSP (including children)
                if ((mpls_topology.HierarchicalLabelSwitchedPaths.Where(h => h.SymbolicPathName == hlsp.SymbolicPathName).Count() == 0 || hlsp.UpdateConfiguration) && hlsp.Configure)
                {
                    // Ansible Tower Client
                    Ansible.TowerClient tc = new Ansible.TowerClient(ansible_tower_uri, ansible_tower_username, ansible_tower_password);

                    var hlsps = new List<Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath>();
                    hlsps.Add(hlsp);

                    dynamic payload = new ExpandoObject();
                    payload.AnsibleTowerHost = hlsp.AnsibleTowerHost;
                    payload.HierarchicalLabelSwitchedPaths = hlsps;

                    var jobId = tc.LaunchJob(hlsp.AnsibleTowerJobTemplateId, payload, hlsp.AnsibleTowerHost);

                    if (jobId != -1)
                    {
                        // Check if job is completed, max 60 times * 5 seconds = 5 min
                        Console.WriteLine("Ansible Tower checking for job {0}.", jobId);
                        var jobCompleted = tc.CheckJobCompleted(jobId);

                        for (int i = 0; i < 60; i++)
                        {
                            Console.WriteLine("Ansible Tower checking for job {0}.", jobId);
                            jobCompleted = tc.CheckJobCompleted(jobId);

                            if (jobCompleted)
                            {
                                Console.WriteLine("Ansible Tower job {0} completed.", jobId);

                                if (tc.CheckJobSuccessful(jobId))
                                {
                                    Console.WriteLine("Ansible Tower job {0} successful.", jobId);

                                    // Update paths through SDN controller
                                    foreach (var lsp in hlsp.Children)
                                    {
                                        if (lsp.Optimise)
                                        {
                                            nitrogen.UpdateLabelSwitchedPath(lsp);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Ansible Tower job " + jobId + " failed.");
                                }

                                break;
                            }

                            System.Threading.Thread.Sleep(1000 * 5);
                        }

                        if (!jobCompleted)
                        {
                            Console.WriteLine("Timed out checking for Ansible Tower job " + jobId + ". Please check Ansible Tower logs.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unable to start Ansible Tower job.");
                    }

                }
                else
                {
                    // Update paths through SDN controller
                    foreach (var lsp in hlsp.Children)
                    {
                        if (lsp.Optimise)
                        {
                            nitrogen.UpdateLabelSwitchedPath(lsp);
                        }
                    }
                }
            }
            else
            {
                if (hlsp.Configure)
                {
                    // Ansible Tower Client
                    Ansible.TowerClient tc = new Ansible.TowerClient(ansible_tower_uri, ansible_tower_username, ansible_tower_password);

                    var hlsps = new List<Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath>();
                    hlsps.Add(hlsp);

                    dynamic payload = new ExpandoObject();
                    payload.AnsibleTowerHost = hlsp.AnsibleTowerHost;
                    payload.HierarchicalLabelSwitchedPaths = hlsps;

                    var jobId = tc.LaunchJob(hlsp.AnsibleTowerJobTemplateId, payload, hlsp.AnsibleTowerHost);

                    if (jobId != -1)
                    {
                        // Check if job is completed, max 60 times * 5 seconds = 5 min 
                        Console.WriteLine("Ansible Tower checking for job {0}.", jobId);
                        var jobCompleted = tc.CheckJobCompleted(jobId);

                        for (int i = 0; i < 60; i++)
                        {
                            Console.WriteLine("Ansible Tower checking for job {0}.", jobId);
                            jobCompleted = tc.CheckJobCompleted(jobId);

                            if (jobCompleted)
                            {
                                Console.WriteLine("Ansible Tower job {0} completed.", jobId);

                                if (tc.CheckJobSuccessful(jobId))
                                {
                                    Console.WriteLine("Ansible Tower job {0} successful.", jobId);

                                }
                                else
                                {
                                    Console.WriteLine("Ansible Tower job " + jobId + " failed.");
                                }

                                break;
                            }

                            System.Threading.Thread.Sleep(1000 * 5);
                        }

                        if (!jobCompleted)
                        {
                            Console.WriteLine("Timed out checking for Ansible Tower job " + jobId + ". Please check Ansible Tower logs.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unable to start Ansible Tower job.");
                    } 
                }
            }
        }

        private static void HLSP_Processor(
            List<Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath> hlsps,
            Topology.IGP.Topology igp_topology, Topology.MPLS.Topology mpls_topology,
            string controller_uri, string controller_username, string controller_password,
            string ansible_tower_uri, string ansible_tower_username, string ansible_tower_password)
        {

            bool allSameAnsibleTowerHost = !hlsps
                .Select(h => new { h.AnsibleTowerHost, h.AnsibleTowerJobTemplateId, h.Configure, h.UpdateConfiguration, h.Delete })
                .Distinct().Skip(1)
                .Any();

            if (allSameAnsibleTowerHost && hlsps.First().Configure && hlsps.First().UpdateConfiguration)
            {
                // Ansible Tower Client
                Ansible.TowerClient tc = new Ansible.TowerClient(ansible_tower_uri, ansible_tower_username, ansible_tower_password);

                dynamic payload = new ExpandoObject();
                payload.AnsibleTowerHost = hlsps.First().AnsibleTowerHost;
                payload.HierarchicalLabelSwitchedPaths = hlsps;

                var jobId = tc.LaunchJob(hlsps.First().AnsibleTowerJobTemplateId, payload, hlsps.First().AnsibleTowerHost);

                if (jobId != -1)
                {
                    // Check if job is completed, max 90 times * 10 seconds = 15 min 
                    Console.WriteLine("Ansible Tower checking for job {0}.", jobId);
                    var jobCompleted = tc.CheckJobCompleted(jobId);

                    for (int i = 0; i < 90; i++)
                    {
                        Console.WriteLine("Ansible Tower checking for job {0}.", jobId);
                        jobCompleted = tc.CheckJobCompleted(jobId);

                        if (jobCompleted)
                        {
                            Console.WriteLine("Ansible Tower job {0} completed.", jobId);

                            if (tc.CheckJobSuccessful(jobId))
                            {
                                Console.WriteLine("Ansible Tower job {0} successful.", jobId);

                                if (!hlsps.First().Delete)
                                {
                                    // Update paths through SDN controller

                                    Controllers.OpenDayLight.Nitrogen.ODLClient nitrogen =
                                        new Controllers.OpenDayLight.Nitrogen.ODLClient(controller_uri, controller_username, controller_password);

                                    foreach (var hlsp in hlsps)
                                    {
                                        foreach (var lsp in hlsp.Children)
                                        {
                                            if (lsp.Optimise)
                                            {
                                                nitrogen.UpdateLabelSwitchedPath(lsp);

                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Ansible Tower job " + jobId + " failed.");
                            }

                            break;
                        }

                        System.Threading.Thread.Sleep(1000 * 10);
                    }

                    if (!jobCompleted)
                    {
                        Console.WriteLine("Timed out checking for Ansible Tower job " + jobId + ". Please check Ansible Tower logs.");
                    }
                }
                else
                {
                    Console.WriteLine("Unable to start Ansible Tower job.");
                }
            }
            else
            {
                foreach (var hlsp in hlsps)
                {
                    HLSP_Processor(
                        hlsp,
                        igp_topology, mpls_topology,
                        controller_uri, controller_username, controller_password,
                        ansible_tower_uri, ansible_tower_username, ansible_tower_password
                        );
                }
            }
        }

        public static void IntentProcessor(object result, Topology.MPLS.Topology mpls_topology, Topology.IGP.Topology igp_topology,
            string controller_uri, string controller_username, string controller_password,
            string ansible_tower_uri, string ansible_tower_username, string ansible_tower_password)
        {
            if (result != null)
            {
                if (result.GetType() == typeof(Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath))
                {
                    Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath hlsp =
                        (Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath)result;

                    HLSP_Processor(
                        hlsp, 
                        igp_topology, mpls_topology, 
                        controller_uri, controller_username, controller_password,
                        ansible_tower_uri, ansible_tower_username, ansible_tower_password
                        );
                }
                else
                {
                    if (result.GetType() == typeof(List<Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath>))
                    {
                        List<Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath> hlsps =
                        (List<Topology.MPLS.HierarchicalLabelSwitchedPath.HierarchicalLabelSwitchedPath>)result;

                        HLSP_Processor(
                            hlsps,
                            igp_topology, mpls_topology,
                            controller_uri, controller_username, controller_password,
                            ansible_tower_uri, ansible_tower_username, ansible_tower_password
                            );
                    }
                }
            }
        }
    }
}
