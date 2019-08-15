using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.CodeDom.Compiler;

#region Extra Libraries
using RestSharp;
#endregion

namespace shift.yggdrasil2.Intent
{
    public class IntentCompiler
    {
        public static CompilerResults CompileCSharpString(string code)
        {
            try
            {
                // Compile

                var refs = AppDomain.CurrentDomain.GetAssemblies();
                var refFiles = refs.Where(a => !a.IsDynamic && 
                !a.Location.Contains("netstandard.dll") &&
                !a.Location.Contains("System.ValueTuple.dll") &&
                !a.Location.Contains("Prometheus.NetStandard.dll")).Select(a => a.Location).ToList();

                // Add extra assemblies
                refFiles.Add(typeof(RestClient).Assembly.Location); // Add RestSharp
                refFiles.Add(typeof(QuickGraph.TaggedEdge<object,object>).Assembly.Location); // Add QuickGraph

                foreach (var refFile in refFiles)
                {
                    Console.WriteLine(refFile);
                }

                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                var compileParams = new CompilerParameters(refFiles.ToArray());
                compileParams.GenerateInMemory = true;
                compileParams.GenerateExecutable = false;

                var compilerResults = provider.CompileAssemblyFromSource(compileParams, code);

                foreach (var error in compilerResults.Errors)
                {
                    Console.WriteLine(error);
                }

                return compilerResults;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return null;
            }
        }
    }
}
