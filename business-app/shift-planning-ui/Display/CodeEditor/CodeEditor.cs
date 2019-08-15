using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Text;
using shift.ui.architect.ExtensionMethods;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Diagnostics;


namespace shift.ui.architect.display.CodeEditor
{
    public class CodeEditor
    {
        public bool Error { get; private set; } = false;

        public EasyScintilla.SimpleEditor Editor { get; private set; }

        public shift.yggdrasil2.Topology.IGP.Topology igp_topology { get; set; }

        public shift.yggdrasil2.Topology.MPLS.Topology mpls_topology { get; set; }

        public RichTextBox rtbOutput { get; set; }

        public delegate void OnSavePointLeft();
        public event OnSavePointLeft OnSavePointLeftCallback;

        public delegate void OnSavePointReached();
        public event OnSavePointReached OnSavePointReachedCallback;

        private string FileName = "";

        public delegate void OnExecCompleted(Object res);
        public event OnExecCompleted OnExecCompletedCallback;

        ToolStripProgressBar pbCodeEditor;
        ToolStripLabel lblCodeEditor;

        public Control CreateEditor()
        {

            Panel panelCodeEditor = new Panel();
            EasyScintilla.SimpleEditor codeEditor = new EasyScintilla.SimpleEditor();

            this.Editor = codeEditor;

            codeEditor.SavePointLeft += (s, ea) => {
                this.OnSavePointLeftCallback?.Invoke();
            };

            codeEditor.SavePointReached += (s, ea) =>
            {
                this.OnSavePointReachedCallback?.Invoke();
            };

            ToolStrip tsCodeEditor = new ToolStrip();

            ToolStripButton tsbSave = new ToolStripButton("Save");
            tsbSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSave.Click += TsbSave_Click;

            ToolStripButton tsbRun = new ToolStripButton("Run");
            tsbRun.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbRun.Click += TsbRun_Click;

            ToolStripButton tsbRunTests = new ToolStripButton("Run Tests");
            tsbRunTests.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbRunTests.Click += TsbRunTests_Click;

            ToolStripDropDownButton tsddbBoilerplate = new ToolStripDropDownButton("Boilerplate");
            tsddbBoilerplate.DisplayStyle = ToolStripItemDisplayStyle.Text;

            ToolStripItem tsiBoilerplateHLSP = new ToolStripMenuItem();
            tsiBoilerplateHLSP.Text = "Hierarchical LSP";
            tsiBoilerplateHLSP.DisplayStyle = ToolStripItemDisplayStyle.Text;

            tsiBoilerplateHLSP.Click += (a, ea) => { 
                // Editor.Text = shift.ui.architect.Code.ShiftCode.Intent.boilerplate;
            };

            tsddbBoilerplate.DropDownItems.Add(tsiBoilerplateHLSP);

            tsCodeEditor.Items.Add(tsbSave);
            tsCodeEditor.Items.Add(tsbRun);
            tsCodeEditor.Items.Add(tsbRunTests);
            tsCodeEditor.Items.Add(tsddbBoilerplate);


            StatusStrip ssCodeEditor = new StatusStrip();
            ssCodeEditor.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;

            pbCodeEditor = new ToolStripProgressBar();
            pbCodeEditor.Style = ProgressBarStyle.Marquee;
            pbCodeEditor.Alignment = ToolStripItemAlignment.Left;
            pbCodeEditor.Visible = false;

            ssCodeEditor.Items.Add(pbCodeEditor);

            lblCodeEditor = new ToolStripLabel("Ready");
            lblCodeEditor.DisplayStyle = ToolStripItemDisplayStyle.Text;
            lblCodeEditor.Alignment = ToolStripItemAlignment.Right;

            ssCodeEditor.Items.Add(lblCodeEditor);

            codeEditor.Styler = new EasyScintilla.Stylers.DarkCSharpStyler();
            codeEditor.CharAdded += CodeEditor_CharAdded;
            codeEditor.Name = "codeEditor";

            panelCodeEditor.Controls.Add(codeEditor);
            codeEditor.Location = new System.Drawing.Point(3, 28);
            codeEditor.Size = new System.Drawing.Size(panelCodeEditor.Width - 6, panelCodeEditor.Height - 48);
            codeEditor.Anchor = (AnchorStyles.Top| AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right);

            panelCodeEditor.Controls.Add(tsCodeEditor);
            tsCodeEditor.Dock = DockStyle.Top;

            panelCodeEditor.Controls.Add(ssCodeEditor);
            ssCodeEditor.Dock = DockStyle.Bottom;

            return panelCodeEditor;
        }

        private void TsbSave_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(FileName, Editor.Text);
                Editor.SetSavePoint();
            }
            catch (Exception)
            {
                MessageBox.Show("There was an error saving the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TsbRun_Click(object sender, EventArgs e)
        {
            pbCodeEditor.Visible = true;
            lblCodeEditor.Text = "Busy";
            Application.DoEvents();

            var start = DateTime.Now;

            // Redirect App Output

            using (var memStream = new MemoryStream())
            {
                TextWriter tw = new StreamWriter(memStream);

                Console.SetOut(tw);

                try
                {
                    // Compile

                    var compilerResults = yggdrasil2.Intent.IntentCompiler.CompileCSharpString(this.Editor.Text);

                    // Execute
                    
                    var tempType = compilerResults.CompiledAssembly.GetType("ShiftPolicy");

                    object[] parameters = {
                        igp_topology,
                        mpls_topology,
                        new yggdrasil2.PathComputation.PathComputation.YggdrasilNM2(),
                        null
                    };

                    var intentResult = tempType.GetMethod("Intent").Invoke(null, parameters);

                    Console.WriteLine("Response Type: {0}", intentResult.GetType());

                    this.OnExecCompletedCallback?.Invoke(intentResult);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.WriteLine("\n\t\t Elapsed Time: {0}", DateTime.Now - start);

                // Display Output

                tw.Flush();
                memStream.Seek(0, SeekOrigin.Begin);

                using (var streamReader = new StreamReader(memStream))
                {
                    rtbOutput.AppendAutoScroll(streamReader.ReadToEnd());
                }

                Console.OpenStandardOutput();
            }

            pbCodeEditor.Visible = false;
            lblCodeEditor.Text = "Ready";
        }

        private void TsbRunTests_Click(object sender, EventArgs e)
        {
            pbCodeEditor.Visible = true;
            lblCodeEditor.Text = "Busy";
            Application.DoEvents();

            var start = DateTime.Now;

            // Redirect App Output

            using (var memStream = new MemoryStream())
            {
                TextWriter tw = new StreamWriter(memStream);

                Console.SetOut(tw);

                try
                {
                    // Compile

                    var compilerResults = yggdrasil2.Intent.IntentCompiler.CompileCSharpString(this.Editor.Text);

                    // Execute


                    var tempType = compilerResults.CompiledAssembly.GetType("ShiftPolicy");

                    object[] parameters = { igp_topology, mpls_topology, new yggdrasil2.PathComputation.PathComputation.YggdrasilNM2() };

                    var intentTestResult = tempType.GetMethod("Test").Invoke(null, parameters); // Move to own button and print results

                    Console.WriteLine("Response Type: {0}", intentTestResult.GetType());

                    if (intentTestResult.GetType() == typeof(yggdrasil2.Intent.IntentTester))
                    {
                        Console.WriteLine("Intent Testing Result:\n");

                        yggdrasil2.Intent.IntentTester tester = (yggdrasil2.Intent.IntentTester)intentTestResult;

                        foreach (var test in tester.TestResults.Tests)
                        {
                            Console.Write("[" + test.Message + "] [" + test.TestType  + "]");
                            if (test.Success)
                            {
                                Console.WriteLine(".... \u001b[32mPASS\u001b[0m");
                            }
                            else
                            {
                                Console.WriteLine(".... \u001b[31mFAIL\u001b[0m");
                            }
                        }

                        Console.WriteLine("\n\t I ran " + tester.TestResults.Tests.Count + " tests:");
                        Console.WriteLine("\n\t\t \u001b[32mSUCCESS: " + tester.TestResults.Tests.Where(t => t.Success == true).Count() + "\u001b[0m");
                        Console.WriteLine("\n\t\t \u001b[31mFAILURE: " + tester.TestResults.Tests.Where(t => t.Success == false).Count() + "\u001b[0m");

                        Console.WriteLine("\n\t\t Elapsed Time: {0}", DateTime.Now - start);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                // Display Output

                tw.Flush();
                memStream.Seek(0, SeekOrigin.Begin);

                using (var streamReader = new StreamReader(memStream))
                {
                    rtbOutput.AppendAutoScroll(streamReader.ReadToEnd());
                }

                Console.OpenStandardOutput();
            }

            pbCodeEditor.Visible = false;
            lblCodeEditor.Text = "Ready";
        }


        public async void LoadFile(EasyScintilla.SimpleEditor codeEditor, string file_name)
        {

            try
            {
                var loader = codeEditor.CreateLoader(256);
                if (loader == null)
                    throw new ApplicationException("Unable to create loader.");

                var cts = new CancellationTokenSource();
                var document = await LoadFileAsync(loader, file_name, cts.Token);
                codeEditor.Document = document;

                // Every document starts with a reference count of 1. Assigning it to Scintilla increased that to 2.
                // To let Scintilla control the life of the document, we'll drop it back down to 1.
                codeEditor.ReleaseDocument(document);
                codeEditor.Styler = new EasyScintilla.Stylers.DarkCSharpStyler();

                FileName = file_name;

            }
            catch (OperationCanceledException)
            {
                this.Error = true;
            }
            catch (Exception)
            {
                this.Error = true;

                MessageBox.Show("There was an error loading the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void CodeEditor_CharAdded(object sender, ScintillaNET.CharAddedEventArgs e)
        {
            var scintilla = (ScintillaNET.Scintilla)sender;

            // Find the word start
            var currentPos = scintilla.CurrentPosition;
            var wordStartPos = scintilla.WordStartPosition(currentPos, true);

            // Display the autocompletion list
            var lenEntered = currentPos - wordStartPos;
            if (lenEntered > 0)
            {
                if (!scintilla.AutoCActive)
                {
                    try
                    {
                        scintilla.SearchFlags = (ScintillaNET.SearchFlags.MatchCase | ScintillaNET.SearchFlags.WholeWord);

                        string word = scintilla.GetTextRange(scintilla.WordStartPosition(currentPos, true), lenEntered);

                        switch (word)
                        {
                            case "igpTopo":

                                scintilla.AutoCShow(lenEntered, DumbSense(word, "shift.yggdrasil2.Topology.IGP.Topology"));

                                break;
                            case "mplsTopo":

                                scintilla.AutoCShow(lenEntered, DumbSense(word, "shift.yggdrasil2.Topology.MPLS.Topology"));

                                break;
                            case "nm2":

                                scintilla.AutoCShow(lenEntered, DumbSense(word, "shift.yggdrasil2.PathComputation.PathComputation+YggdrasilNM2"));

                                break;
                            default:

                                int scopeStart = scintilla.Text.LastIndexOf("{", wordStartPos);
                                int scopeEnd = scintilla.Text.IndexOf("}", wordStartPos);

                                scintilla.TargetStart = scopeStart;
                                scintilla.TargetEnd = scopeEnd;

                                int firstSeen = scintilla.SearchInTarget(word);

                                string typeName = scintilla.GetTextRange(scintilla.Text.LastIndexOf("\n", firstSeen), firstSeen - scintilla.Text.LastIndexOf("\n", firstSeen)).Trim();

                                scintilla.TargetStart = 0;
                                scintilla.TargetEnd = scopeStart;

                                firstSeen = scintilla.SearchInTarget(typeName);

                                string namespaceName = scintilla.GetTextRange(scintilla.Text.IndexOf("=", firstSeen), scintilla.Text.IndexOf("\n", firstSeen) - scintilla.Text.IndexOf("=", firstSeen)).Trim("= ; \r".ToCharArray());

                                scintilla.AutoCShow(lenEntered, DumbSense(word, namespaceName));

                                break;
                        }


                    }
                    catch (Exception)
                    {
                        scintilla.AutoCShow(lenEntered, "HierarchicalLabelSwitchedPath((string)SymbolicPathName) LabelSwitchedPath((string)ParentId) abstract as base break case catch checked continue default delegate do else event explicit extern false finally fixed for foreach goto if implicit in interface internal is lock namespace new null object operator out override params private protected public readonly ref return sealed sizeof stackalloc switch this throw true try typeof unchecked unsafe using virtual while");
                    }
                }
            }
        }

        public async Task<ScintillaNET.Document> LoadFileAsync(ScintillaNET.ILoader loader, string path, CancellationToken cancellationToken)
        {
            try
            {
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                using (var reader = new StreamReader(file))
                {
                    var count = 0;
                    var buffer = new char[4096];
                    while ((count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();

                        // Add the data to the document
                        if (!loader.AddData(buffer, count))
                            throw new IOException("The data could not be added to the loader.");
                    }

                    return loader.ConvertToDocument();
                }
            }
            catch
            {
                this.Error = true;
                loader.Release();
                throw;
            }
        }

        private static string DumbSense(string name, string fullname)
        {
            var propInfo = Type.GetType(fullname).GetProperties();
            var methodInfo = Type.GetType(fullname).GetMethods();

            StringBuilder autoString = new StringBuilder();

            foreach (var prop in propInfo)
            {
                autoString.Append(name);
                autoString.Append(".");
                autoString.Append(prop.Name);
                autoString.Append("=(");
                

                if(Nullable.GetUnderlyingType(prop.PropertyType) != null)
                {
                    autoString.Append("Nullable<" + Nullable.GetUnderlyingType(prop.PropertyType).Name + ">");
                } else
                {
                    if (prop.PropertyType.IsGenericType)
                    {
                        
                        if(prop.PropertyType.GetInterface(nameof(System.Collections.IEnumerable)) != null)
                        {
                            autoString.Append(prop.PropertyType.Name.TrimEnd("`1".ToCharArray()) + "<");
                            foreach (var a in prop.PropertyType.GetGenericArguments())
                            {
                                autoString.Append(a.Name);
                            }
                            autoString.Append(">");
                        }
                    } else
                    {
                        autoString.Append(prop.PropertyType.Name);
                    }
                }

                autoString.Append(") ");
            }

            foreach (var method in methodInfo)
            {
                var p = method.GetParameters();

                autoString.Append(name);
                autoString.Append(".");
                autoString.Append(method.Name.TrimStart("get_set_".ToCharArray()));

                autoString.Append("(");

                foreach (var parameter in p)
                {
                    autoString.Append("(");

                    if (Nullable.GetUnderlyingType(parameter.ParameterType) != null)
                    {
                        autoString.Append("Nullable<" + Nullable.GetUnderlyingType(parameter.ParameterType).Name + ">");
                    }
                    else
                    {
                        if (parameter.ParameterType.IsGenericType)
                        {

                            if (parameter.ParameterType.GetInterface(nameof(System.Collections.IEnumerable)) != null)
                            {
                                autoString.Append(parameter.ParameterType.Name.TrimEnd("`1".ToCharArray()) + "<");
                                foreach (var a in parameter.ParameterType.GetGenericArguments())
                                {
                                    autoString.Append(a.Name);
                                }
                                autoString.Append(">");
                            }
                        }
                        else
                        {
                            autoString.Append(parameter.ParameterType.Name);
                        }
                    }
                    
                    autoString.Append(")" + parameter.Name);
                }

                autoString.Append(")");

                autoString.Append(" ");
            }

            return autoString.ToString();
        }

    }
}
