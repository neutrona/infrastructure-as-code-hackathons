using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#region JSON.NET
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using J = Newtonsoft.Json.JsonPropertyAttribute;
#endregion

namespace shift.yggdrasil2.cicd
{
    [Serializable]
    class IntentJob
    {

        [J("FileName")]public string FileName { get; private set; }
        [J("Base64IntentCode")]public string Base64IntentCode { get; private set; }
        [J("Period")] public int Period { get; private set; }
        [J("Due")] public DateTime Due { get; private set; }
        [J("ValidBefore")] public DateTime ValidBefore { get; private set; }
        [J("RequiresIteration")] public bool RequiresIteration { get; private set; }
        [J("IterateNode")] public Topology.Node.Node IterateNode { get; private set; }


        [JsonIgnore] public string PlainIntentCode { get { return Base64Decode(this.Base64IntentCode); } }


        public DateTime NextRun()
        {
            if(DateTime.Now < this.Due)
            {
                return this.Due;
            }
            else
            {
                if (this.Period > 0)
                {
                    this.Due = DateTime.Now.AddMinutes(this.Period);
                    return DateTime.Now;
                }
                else
                {
                    this.Due = DateTime.MaxValue;
                    return DateTime.Now;
                }
            }
        }

        public IntentJob()
        {

        }

        // Without Iterate Node
        public IntentJob(string fileName, string intentCode, int period, DateTime validBefore, bool requiresIteration)
        {
            this.FileName = fileName;
            this.Base64IntentCode = Base64Encode(intentCode);
            this.Period = period;
            this.ValidBefore = validBefore;

            this.Due = DateTime.Now;

            this.RequiresIteration = requiresIteration;

            this.IterateNode = null;
        }

        // With Iterate Node
        public IntentJob(string fileName, string intentCode, int period, DateTime validBefore, bool requiresIteration, Topology.Node.Node iterateNode)
        {
            this.FileName = fileName;
            this.Base64IntentCode = Base64Encode(intentCode);
            this.Period = period;
            this.ValidBefore = validBefore;

            this.Due = DateTime.Now;

            this.RequiresIteration = requiresIteration;

            this.IterateNode = iterateNode;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

    }
}
