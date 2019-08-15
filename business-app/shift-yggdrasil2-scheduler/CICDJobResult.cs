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

#region JSON.NET
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using J = Newtonsoft.Json.JsonPropertyAttribute;
#endregion

namespace shift.yggdrasil2.cicd.scheduler
{
    [Serializable]
    class CICDJobResult
    {
        [J("JobType")] [JsonConverter(typeof(StringEnumConverter))] public JobType JobType { get; set; }
        [J("FileName")] public string FileName { get; set; }
        [J("Success")] public bool Success { get; private set; }
        [J("CompilerErrors")] public CompilerErrorCollection CompilerErrors { get; private set; }
        [J("IntentTesterResults")] public Intent.IntentTesterResults IntentTesterResults { get; private set; }
        [J("IntentJob")] public IntentJob IntentJob { get; private set; }
        [J("LastExceptionMessage")] public string LastExceptionMessage { get; private set; }


        // Job Type: Build
        public CICDJobResult(CompilerResults compilerResults, JobType jobType, string fileName)
        {
            switch (jobType)
            {
                case JobType.Build:
                    this.JobType = jobType;
                    this.CompilerErrors = compilerResults.Errors;
                    this.FileName = fileName;

                    if (compilerResults.Errors.Count > 0)
                    {
                        this.Success = false;
                    }
                    else
                    {
                        this.Success = true;
                    }
                    break;
                case JobType.Test:
                    throw new Exception("JobType.Test: Incorrect number of parameters.");
                case JobType.Deploy:
                    throw new Exception("JobType.Deploy: Incorrect number of parameters.");
                default:
                    break;
            }
        }

        // Job Type: Test
        public CICDJobResult(CompilerResults compilerResults, JobType jobType,
            Topology.IGP.Topology igp_topology, Topology.MPLS.Topology mpls_topology, string fileName)
        {
            switch (jobType)
            {
                case JobType.Build:
                    throw new Exception("JobType.Build: Incorrect number of parameters.");
                case JobType.Test:
                    this.JobType = jobType;
                    this.CompilerErrors = compilerResults.Errors;
                    this.FileName = fileName;


                    if (compilerResults.Errors.Count > 0)
                    {
                        this.Success = false;
                    }
                    else
                    {
                        try
                        {
                            var tempType = compilerResults.CompiledAssembly.GetType("ShiftPolicy");
                            object[] parameters = { igp_topology, mpls_topology, new yggdrasil2.PathComputation.PathComputation.YggdrasilNM2() };

                            #region Mandatory Properties
                            bool enabled = (bool)tempType.GetProperty("Enabled").GetGetMethod().Invoke(null, null);
                            int period = (int)tempType.GetProperty("Period").GetGetMethod().Invoke(null, null);
                            DateTime validBefore = (DateTime)tempType.GetProperty("ValidBefore").GetGetMethod().Invoke(null, null);

                            List<Topology.Node.Node> iterateNodes = (List<Topology.Node.Node>)tempType.GetMethod("IterateNodes").Invoke(null, new object[] { igp_topology });
                            #endregion

                            if (enabled && validBefore > DateTime.Now)
                            {
                                var intentTestResult = tempType.GetMethod("Test").Invoke(null, parameters);

                                this.IntentTesterResults = ((Intent.IntentTester)intentTestResult).TestResults;

                                if (((Intent.IntentTester)intentTestResult).TestResults.Errors.Count > 0)
                                {
                                    this.Success = false;
                                }
                                else
                                {
                                    this.Success = true;
                                }
                            }
                            else
                            {
                                this.Success = true; // true = skip
                            }
                        }
                        catch (Exception ex)
                        {
                            // Console.WriteLine(ex.Message + " at " + MethodBase.GetCurrentMethod().Name);
                            this.Success = false;
                            this.LastExceptionMessage = ex.Message;
                        }
                    }
                    break;
                case JobType.Deploy:
                    throw new Exception("JobType.Deploy: Incorrect number of parameters.");
                default:
                    break;
            }


        }

        // Job Type: Deploy
        public CICDJobResult(CompilerResults compilerResults, JobType jobType, Topology.IGP.Topology igp_topology, string fileName)
        {
            switch (jobType)
            {
                case JobType.Build:
                    throw new Exception("JobType.Build: Incorrect number of parameters.");
                case JobType.Test:
                    throw new Exception("JobType.Test: Incorrect number of parameters.");
                case JobType.Deploy:
                    this.JobType = jobType;
                    this.CompilerErrors = compilerResults.Errors;
                    this.FileName = fileName;

                    if (compilerResults.Errors.Count > 0)
                    {
                        this.Success = false;
                        break;
                    }
                    else
                    {
                        try
                        {
                            var tempType = compilerResults.CompiledAssembly.GetType("ShiftPolicy");

                            #region Mandatory Properties
                            bool enabled = (bool)tempType.GetProperty("Enabled").GetGetMethod().Invoke(null, null);
                            int period = (int)tempType.GetProperty("Period").GetGetMethod().Invoke(null, null);
                            DateTime validBefore = (DateTime)tempType.GetProperty("ValidBefore").GetGetMethod().Invoke(null, null);

                            List<Topology.Node.Node> iterateNodes = (List<Topology.Node.Node>)tempType.GetMethod("IterateNodes").Invoke(null, new object[] { igp_topology });
                            #endregion

                            bool requiresIteration = false;

                            if (iterateNodes.Count > 0)
                            {
                                requiresIteration = true;
                            }

                            if (enabled && validBefore > DateTime.Now)
                            {
                                this.IntentJob = new IntentJob(
                                    fileName: fileName,
                                    intentCode: File.ReadAllText(fileName),
                                    period: period,
                                    validBefore: validBefore,
                                    requiresIteration: requiresIteration
                                    );
                            }
                        }
                        catch (Exception ex)
                        {
                            // Console.WriteLine(ex.Message + " at " + MethodBase.GetCurrentMethod().Name);
                            this.Success = false;
                            this.LastExceptionMessage = ex.Message;
                        }

                        this.Success = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
