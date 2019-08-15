using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using shift.ui.Architect.display.controls;

using shift.ui.architect.ExtensionMethods;


#region DOCK UI
using WeifenLuo.WinFormsUI.Docking;
#endregion

#region NShape
using Dataweb;
using Dataweb.NShape;
using Dataweb.NShape.Advanced;
using Dataweb.NShape.Layouters;
using Dataweb.NShape.GeneralShapes;
#endregion

#region Neo4J
using Neo4j.Driver.V1;
#endregion

#region QuickGraph
using QuickGraph;
using QuickGraph.Algorithms.ShortestPath;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.RankedShortestPath;
using QuickGraph.Collections;
using QuickGraph.Algorithms;
#endregion

#region JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endregion

#region LibGit2Sharp

using LibGit2Sharp;

#endregion

#region SHIFT UI
using shift.ui.display;
#endregion

#region SHIFT YGGDRASIL2
using shift.yggdrasil2;
#endregion


using GraphX;
using GraphX.Controls;
using GraphX.PCL.Common.Models;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using GraphX.PCL.Logic.Models;

namespace shift.ui.architect
{
    public partial class FormMain : Form
    {
        #region Config

        static Config config;

        #endregion

        #region Log Options
        LogOptions logOptions = new LogOptions();
        #endregion

        #region Git Repository

        string remoteRepo;
        string repoLocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "shift", "code");
        Dictionary<string, DockContent> openFileTabs = new Dictionary<string, DockContent>();

        SecureUsernamePasswordCredentials gitCredentials = new SecureUsernamePasswordCredentials();

        FileSystemWatcher watcher = new FileSystemWatcher();

        #endregion

        #region DOCK UI
        DockContent dockContentNetworkDiagram = new DockContent();
        DockContent dockContentRepoExplorer = new DockContent();
        DockContent dockContentOutput = new DockContent();
        DockContent dockContentProperties = new DockContent();
        DockContent dockContentResult = new DockContent();
        DockContent dockContentTunnels = new DockContent();
        #endregion

        #region NShape
        Dataweb.NShape.WinFormsUI.Display displayNetwork = new Dataweb.NShape.WinFormsUI.Display();
        Dataweb.NShape.Controllers.DiagramSetController diagramSetController = new Dataweb.NShape.Controllers.DiagramSetController();
        Dataweb.NShape.Project project = new Project();
        Dataweb.NShape.Advanced.CachedRepository cachedRepository = new CachedRepository();
        Dataweb.NShape.XmlStore xmlStore = new XmlStore();
        Dataweb.NShape.Controllers.ToolSetController toolSetController = new Dataweb.NShape.Controllers.ToolSetController();
        Layer networkLayer = new Layer("NETWORK");
        Layer overlayLayer = new Layer("OVERLAY");

        int diagramHeight = 15000;
        int diagramWidth = 15000;
        #endregion

        #region SHIFT YGGRDASIL2
        // Topology
        static yggdrasil2.Topology.IGP.Topology igp_topology = new yggdrasil2.Topology.IGP.Topology();
        static yggdrasil2.Topology.MPLS.Topology mpls_topology = new yggdrasil2.Topology.MPLS.Topology();

        static ConcurrentQueue<JObject> igp_topology_changes_queue = new ConcurrentQueue<JObject>(); // IGP Topology Buffer
        static bool igp_topology_task_enabled = false;

        static ConcurrentQueue<JObject> mpls_topology_changes_queue = new ConcurrentQueue<JObject>(); // MPLS Topology Buffer
        static bool mpls_topology_task_enabled = false;

        // Messaging
        yggdrasil2.Messaging.ControlQueue cq;

        yggdrasil2.Messaging.IGP.TopologyQueue tq_igp;

        yggdrasil2.Messaging.MPLS.TopologyQueue tq_mpls;

        yggdrasil2.Messaging.PerformanceQueue pq;

        yggdrasil2.Messaging.NodePCCQueue nq;
        #endregion

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            display.login.Login frmLogin = new display.login.Login();
            frmLogin.ShowDialog();

            config = Config.LoadFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, frmLogin.ConfigFile));

            #region SHIFT YGGRDASIL2

            cq = new yggdrasil2.Messaging.ControlQueue(
                brokerHostname: config.MessageBrokerHostname,
                brokerPort: config.MessageBrokerPort,
                brokerUsername: config.MessageBrokerUsername,
                brokerPassword: config.MessageBrokerPassword,
                routingKey: config.ControlQueueRoutingKey);

            tq_igp = new yggdrasil2.Messaging.IGP.TopologyQueue(
                brokerHostname: config.MessageBrokerHostname,
                brokerPort: config.MessageBrokerPort,
                brokerUsername: config.MessageBrokerUsername,
                brokerPassword: config.MessageBrokerPassword,
                routingKey: config.IGPTopologyQueueRoutingKey);

            tq_mpls = new yggdrasil2.Messaging.MPLS.TopologyQueue(
                brokerHostname: config.MessageBrokerHostname,
                brokerPort: config.MessageBrokerPort,
                brokerUsername: config.MessageBrokerUsername,
                brokerPassword: config.MessageBrokerPassword,
                routingKey: config.MPLSTopologyQueueRoutingKey);

            pq = new yggdrasil2.Messaging.PerformanceQueue(
                brokerHostname: config.MessageBrokerHostname,
                brokerPort: config.MessageBrokerPort,
                brokerUsername: config.MessageBrokerUsername,
                brokerPassword: config.MessageBrokerPassword,
                routingKey: config.PerformanceQueueRoutingKey);

            nq = new yggdrasil2.Messaging.NodePCCQueue(
                brokerHostname: config.MessageBrokerHostname,
                brokerPort: config.MessageBrokerPort,
                brokerUsername: config.MessageBrokerUsername,
                brokerPassword: config.MessageBrokerPassword,
                routingKey: config.NodePCCQueueRoutingKey);

            #endregion

            #region Git Repository

            // Remote Repo
            remoteRepo = config.IntentRepositoryURL;

            // Git Login
            var password = new System.Security.SecureString();

            foreach (var c in frmLogin.Password)
            {
                password.AppendChar(c);
            }

            password.MakeReadOnly();

            gitCredentials.Username = frmLogin.Username;
            gitCredentials.Password = password;

            frmLogin.Dispose();


            // Git Repo

            if (Repository.IsValid(repoLocalPath))
            {
                using (var repo = new Repository(repoLocalPath))
                {
                    var remote = repo.Network.Remotes["origin"];
                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

                    try
                    {
                        var po = new PullOptions();

                        po.FetchOptions = new FetchOptions();

                        po.FetchOptions.CredentialsProvider = (_url, _user, _cred) => { return gitCredentials; };

                        Signature signature = new Signature(gitCredentials.Username.ToString(), gitCredentials.Username.ToString(), DateTime.Now);

                        Commands.Pull(repo, signature, po);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Working on local...", MessageBoxButtons.OK);
                    }
                }

                treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher);
            }
            else
            {
                try
                {
                    var co = new CloneOptions();
                    co.CredentialsProvider = (_url, _user, _cred) => { return gitCredentials; };
                    Repository.Clone(remoteRepo, repoLocalPath, co);

                    treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Clone Failed", MessageBoxButtons.OK);
                }
            }

            // Watch repo directory changes

            if (Repository.IsValid(repoLocalPath))
            {
                watcher.Path = repoLocalPath;

                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

                // Add event handlers.
                watcher.Changed += Watcher_Changed;
                watcher.Created += Watcher_Changed;
                watcher.Deleted += Watcher_Changed;
                watcher.Renamed += Watcher_Changed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;
            }

            #endregion

            #region DOCK UI
            // Dock Diagram
            dockContentNetworkDiagram.ShowHint = DockState.Document;
            dockContentNetworkDiagram.TabText = "Network";
            dockContentNetworkDiagram.CloseButton = false;
            dockContentNetworkDiagram.CloseButtonVisible = false;
            dockContentNetworkDiagram.Icon = this.Icon;
            dockContentNetworkDiagram.Show(dockPanelMain);

            dockContentNetworkDiagram.Controls.Add(displayNetwork);
            displayNetwork.Dock = DockStyle.Fill;

            displayNetwork.SendToBack();

            // Dock Repo Explorer
            dockContentRepoExplorer.ShowHint = DockState.DockLeft;
            dockContentRepoExplorer.TabText = "Intent Explorer";
            dockContentRepoExplorer.CloseButton = false;
            dockContentRepoExplorer.CloseButtonVisible = false;
            dockContentRepoExplorer.Icon = this.Icon;
            dockContentRepoExplorer.Show(dockPanelMain);

            dockContentRepoExplorer.Controls.Add(panelRepositoryExplorer);
            panelRepositoryExplorer.Dock = DockStyle.Fill;

            // Dock Output
            dockContentOutput.ShowHint = DockState.DockBottom;
            dockContentOutput.TabText = "Output";
            dockContentOutput.CloseButton = false;
            dockContentOutput.CloseButtonVisible = false;
            dockContentOutput.Icon = this.Icon;
            dockContentOutput.Show(dockPanelMain);

            dockContentOutput.Controls.Add(panelOutput);
            panelOutput.Dock = DockStyle.Fill;

            // Dock Properties
            dockContentProperties.ShowHint = DockState.DockRight;
            dockContentProperties.TabText = "Properties";
            dockContentProperties.CloseButton = false;
            dockContentProperties.CloseButtonVisible = false;
            dockContentProperties.Icon = this.Icon;
            dockContentProperties.Show(dockPanelMain);

            dockContentProperties.Controls.Add(panelProperties);
            panelProperties.Dock = DockStyle.Fill;

            // Dock Result
            dockContentResult.ShowHint = DockState.DockLeft;
            dockContentResult.TabText = "Result";
            dockContentResult.CloseButton = false;
            dockContentResult.CloseButtonVisible = false;
            dockContentResult.Icon = this.Icon;
            dockContentResult.Show(dockPanelMain);

            dockContentResult.Controls.Add(panelResult);
            panelResult.Dock = DockStyle.Fill;

            // Dock Tunnels
            dockContentTunnels.ShowHint = DockState.DockRight;
            dockContentTunnels.TabText = "Tunnels";
            dockContentTunnels.CloseButton = false;
            dockContentTunnels.CloseButtonVisible = false;
            dockContentTunnels.Icon = this.Icon;
            dockContentTunnels.Show(dockPanelMain);

            dockContentTunnels.Controls.Add(panelTunnels);
            panelTunnels.Dock = DockStyle.Fill;
            #endregion

            #region NShape
            // displayNetwork.ShowScrollBars = false;

            diagramSetController.Project = project;
            toolSetController.DiagramSetController = diagramSetController;
            displayNetwork.DiagramSetController = diagramSetController;
            xmlStore.DirectoryName = Path.Combine(Application.StartupPath, "Resources");
            xmlStore.FileExtension = "nspj";
            cachedRepository.Store = xmlStore;
            project.Repository = cachedRepository;
            project.Name = "Circles";
            project.LibrarySearchPaths.Add(Application.StartupPath);
            project.AutoLoadLibraries = true;

            displayNetwork.IsGridVisible = false;
            displayNetwork.IsSheetVisible = false;
            displayNetwork.BackColor = Color.White;

            RoleBasedSecurityManager securityManager = new RoleBasedSecurityManager();

            securityManager.CurrentRole = StandardRole.Operator;
            securityManager.SetPermissions('A', StandardRole.Operator, Permission.None);
            securityManager.SetPermissions('A', StandardRole.Operator, Permission.Layout);
            securityManager.SetPermissions('B', StandardRole.Operator, Permission.None);
            securityManager.SetPermissions('B', StandardRole.Operator, Permission.Layout);

            project.SecurityManager = securityManager;

            project.Open();

            Diagram diagramNetwork = new Diagram("Network");
            diagramNetwork.Height = diagramHeight;
            diagramNetwork.Width = diagramWidth;


            displayNetwork.Diagram = diagramNetwork;
            displayNetwork.ShowDefaultContextMenu = false;

            displayNetwork.ZoomLevel = 5;

            //STYLE

            ((LineStyle)project.Design.LineStyles.Green).LineWidth = 30;
            cachedRepository.Update(project.Design.LineStyles.Green);

            ((LineStyle)project.Design.LineStyles.Red).LineWidth = 30;
            cachedRepository.Update(project.Design.LineStyles.Red);

            ((LineStyle)project.Design.LineStyles.Highlight).LineWidth = 30;
            cachedRepository.Update(project.Design.LineStyles.Highlight);

            ((CapStyle)project.Design.CapStyles.OpenArrow).CapSize = 60;
            cachedRepository.Update(project.Design.CapStyles.OpenArrow);

            //NETWORK LAYER
            displayNetwork.Diagram.Layers.Add(networkLayer);
            //

            //TUNNEL OVERLAY LAYER
            displayNetwork.Diagram.Layers.Add(overlayLayer);
            //

            displayNetwork.UserMessage += DisplayNetwork_UserMessage; ;
            displayNetwork.ShapesRemoved += DisplayNetwork_ShapesRemoved; ;
            displayNetwork.ShapeClick += DisplayNetwork_ShapeClick; ;
            displayNetwork.ShapeDoubleClick += DisplayNetwork_ShapeDoubleClick; ;
            displayNetwork.MouseClick += DisplayNetwork_MouseClick; ;
            #endregion

            #region SHIFT BACKEND CONNECTION

            // Messaging

            // IGP Topology Queue Task
            var igp_topology_queue_task = new Task(() =>
            {
                while (igp_topology_task_enabled)
                {
                    while (!igp_topology_changes_queue.IsEmpty)
                    {
                        if (igp_topology_changes_queue.TryDequeue(out JObject data))
                        {
                            if (logOptions.DebugIGPTopology) { richTextBoxOutput.AppendParagraphAutoScroll("IGP Topology Change: " + data); }
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
                            if (logOptions.DebugMPLSTopology) { richTextBoxOutput.AppendParagraphAutoScroll("MPLS Topology Change: " + data); }
                            mpls_topology.Update(data);
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
                yggdrasil2.data.Neo4J Neo4J = new yggdrasil2.data.Neo4J
                {
                    Neo4J_URI = config.Neo4J_URI,
                    Neo4J_User = config.Neo4J_User,
                    Neo4J_Password = config.Neo4J_Password
                };


                ToggleProgressBarMain(true);

                Neo4J.LoadIGPTopology(igp_topology);
                mpls_topology = Neo4J.LoadMPLSTopology();

                ToggleProgressBarMain(false);

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
                yggdrasil2.data.Neo4J Neo4J = new yggdrasil2.data.Neo4J
                {
                    Neo4J_URI = config.Neo4J_URI,
                    Neo4J_User = config.Neo4J_User,
                    Neo4J_Password = config.Neo4J_Password
                };


                ToggleProgressBarMain(true);

                Neo4J.LoadIGPTopology(igp_topology);
                mpls_topology = Neo4J.LoadMPLSTopology();

                ToggleProgressBarMain(false);

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

                if (logOptions.DebugPerformanceData) { richTextBoxOutput.AppendParagraphAutoScroll("Incoming Performance Data: " + data); }

                igp_topology.Update(data);
            };

            // Node PCC Queue Event Setup
            nq.OnNodePCCUpdateCallback += (data) =>
            {
                // Console.WriteLine("Node PCC Update: " + data);
                if (logOptions.DebugNodePCC) { richTextBoxOutput.AppendParagraphAutoScroll("Incoming Node PCC Change: " + data); }

                igp_topology.Update(data);
            };

            // Control Queue Connect
            cq.Connect();

            #endregion

        }

        #region Repo Directory Watcher
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (treeViewRepositoryExplorer.InvokeRequired)
            {
                treeViewRepositoryExplorer.BeginInvoke(new MethodInvoker(() => treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher)));

                if (File.Exists(e.FullPath))
                {
                    FileAttributes fileAttributes = File.GetAttributes(e.FullPath);

                    if (fileAttributes.HasFlag(FileAttributes.Directory))
                    {
                        treeViewRepositoryExplorer.BeginInvoke(new MethodInvoker(() => treeViewRepositoryExplorer.ExpandAll()));
                    }
                }
            }
            else
            {
                treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher);

                FileAttributes fileAttributes = File.GetAttributes(e.FullPath);

                if (fileAttributes.HasFlag(FileAttributes.Directory))
                {
                    treeViewRepositoryExplorer.ExpandAll();
                }
            }
        }
        #endregion

        #region Topology Callbacks

        private void Igp_topology_OnNodeUpdateCallback(yggdrasil2.Topology.Node.Node node)
        {
            shift.ui.display.Render.RenderNode(node, displayNetwork);
        }

        private void Igp_topology_OnLinkUpdateCallback(yggdrasil2.Topology.IGP.Link.Link link)
        {
            shift.ui.display.Render.RenderRelationship(link, displayNetwork);
        }


        private static void Igp_topology_OnLinkUpCallback(yggdrasil2.Topology.IGP.Link.Link link)
        {

        }

        private static void Igp_topology_OnLinkDownCallback(yggdrasil2.Topology.IGP.Link.Link link)
        {

        }

        #endregion

        #region NShape Layouter

        private void RunLayouter(int maxSeconds, Dataweb.NShape.WinFormsUI.Display Display)
        {
            try
            {
                RepulsionLayouter repulsionLayouter = new RepulsionLayouter(project);
                //repulsionLayouter.SpringRate = 20;
                repulsionLayouter.Repulsion = 200;
                repulsionLayouter.RepulsionRange = 1000;
                //repulsionLayouter.Friction = 20;
                repulsionLayouter.Mass = 100;
                repulsionLayouter.AllShapes = Display.Diagram.Shapes;
                repulsionLayouter.Shapes = Display.Diagram.Shapes;

                repulsionLayouter.Prepare();

                repulsionLayouter.Execute(maxSeconds);

                repulsionLayouter.Fit(100, 100, Display.Diagram.Width, Display.Diagram.Height);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Display Events
        private void DisplayNetwork_MouseClick(object sender, MouseEventArgs e)
        {
        }

        private void DisplayNetwork_ShapeDoubleClick(object sender, Dataweb.NShape.Controllers.DiagramPresenterShapeClickEventArgs e)
        {
        }

        private void DisplayNetwork_ShapeClick(object sender, Dataweb.NShape.Controllers.DiagramPresenterShapeClickEventArgs e)
        {
            if (e.Shape.Tag != null && e.Shape.Tag.GetType() == typeof(shift.ui.display.UI.ShapeTag))
            {

                shift.ui.display.UI.ShapeTag shapeTag = (shift.ui.display.UI.ShapeTag)e.Shape.Tag;

                if (shapeTag.Object != null)
                {
                    if(shapeTag.Object.GetType() == typeof(shift.yggdrasil2.Topology.IGP.Link.Link))
                    {
                        shift.yggdrasil2.Topology.IGP.Link.Link link = (shift.yggdrasil2.Topology.IGP.Link.Link)shapeTag.Object;

                        propertyGridProperties.ShowCustomProperties = true;
                        propertyGridProperties.Item.Clear();

                        if (link.LastEvent != null)
                        {
                            propertyGridProperties.Item.Add("Last Event", Render.UnixTimeStampToDateTime((long)link.LastEvent, true), true, "Timestamp", "Last Event", true);
                        }

                        if(link.FirstSeen != null)
                        {
                            propertyGridProperties.Item.Add("First Seen", ui.display.Render.UnixTimeStampToDateTime((long)link.FirstSeen, true), true, "Timestamp", "First Event", true);
                        }

                        if(link.Rtt != null)
                        {
                            propertyGridProperties.Item.Add("Round Trip Time [ms]", (decimal)link.Rtt/1000, true, "Performance", "Round Trip Time in milliseconds", true);
                        }

                        propertyGridProperties.Item.Add("Raw", JsonConvert.DeserializeObject(JsonConvert.SerializeObject(link)), true, "Link", "Raw", true);
                        propertyGridProperties.Item[propertyGridProperties.Item.Count - 1].IsBrowsable = true;
                        propertyGridProperties.Item[propertyGridProperties.Item.Count - 1].BrowsableLabelStyle = PropertyGridEx.BrowsableTypeConverter.LabelStyle.lsEllipsis;

                        propertyGridProperties.Refresh();
                    }

                    if (shapeTag.Object.GetType() == typeof(shift.yggdrasil2.Topology.Node.Node))
                    {
                        shift.yggdrasil2.Topology.Node.Node node = (shift.yggdrasil2.Topology.Node.Node)shapeTag.Object;

                        propertyGridProperties.ShowCustomProperties = true;
                        propertyGridProperties.Item.Clear();

                        if (node.LastEvent != null)
                        {
                            propertyGridProperties.Item.Add("Last Event", ui.display.Render.UnixTimeStampToDateTime((long)node.LastEvent, true), true, "Timestamp", "Last Event", true);
                        }

                        if (node.FirstSeen != null)
                        {
                            propertyGridProperties.Item.Add("First Seen", ui.display.Render.UnixTimeStampToDateTime((long)node.FirstSeen, true), true, "Timestamp", "First Event", true);
                        }

                        propertyGridProperties.Item.Add("Raw", JsonConvert.DeserializeObject(JsonConvert.SerializeObject(node)), true, "Node", "Raw", true);
                        propertyGridProperties.Item[propertyGridProperties.Item.Count - 1].IsBrowsable = true;
                        propertyGridProperties.Item[propertyGridProperties.Item.Count - 1].BrowsableLabelStyle = PropertyGridEx.BrowsableTypeConverter.LabelStyle.lsEllipsis;
                        
                        propertyGridProperties.Refresh();

                        propertyGridProperties.ExpandAllGridItems();
                    }
                    
                }

                

            }
        }

        private void DisplayNetwork_ShapesRemoved(object sender, Dataweb.NShape.Controllers.DiagramPresenterShapesEventArgs e)
        {
        }

        private void DisplayNetwork_UserMessage(object sender, Dataweb.NShape.Controllers.UserMessageEventArgs e)
        {
        }
        #endregion

        #region Menu

        private void automaticToolStripMenuItem_Click(object sender, EventArgs e)
        {

            var g = new BidirectionalGraph<DataVertex, DataEdge>();

            foreach (var link in igp_topology.Links)
            {
                var src = g.Vertices.Where(v => v.Text == link.SourceNode).SingleOrDefault();

                if (src == null)
                {
                    src = new DataVertex(link.SourceNode);
                    g.AddVertex(src);
                }

                var dst = g.Vertices.Where(v => v.Text == link.TargetNode).SingleOrDefault();

                if (dst == null)
                {
                    dst = new DataVertex(link.TargetNode);
                    g.AddVertex(dst);
                }

                
               

                g.AddEdge(new DataEdge(src, dst));
                g.AddEdge(new DataEdge(dst, src));
            }


            var logic = new GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>();
            GraphAreaExample _gArea = new GraphAreaExample() { LogicCore = logic };
            logic.Graph = g;
            logic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.LinLog;
            logic.DefaultLayoutAlgorithmParams = logic.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.LinLog);

            _gArea.GenerateGraph(true, true);

            //((LinLogLayoutParameters)logic.DefaultLayoutAlgorithmParams). = 100;
            logic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            logic.DefaultOverlapRemovalAlgorithmParams = logic.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)logic.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
            ((OverlapRemovalParameters)logic.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;
            logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
            logic.AsyncAlgorithmCompute = false;
            _gArea.RelayoutFinished += _gArea_RelayoutFinished;

            _gArea.RelayoutGraph(true);

            // RunLayouter(10, displayNetwork);
        }

        private void _gArea_RelayoutFinished(object sender, EventArgs e)
        {
            foreach (var vp in ((GraphAreaExample)sender).GetVertexPositions())
            {
                var shape = displayNetwork.Diagram.Shapes.Where(s => s.Tag != null && s.Tag.GetType() == typeof(UI.ShapeTag) && ((UI.ShapeTag)s.Tag).Id == vp.Key.Text).SingleOrDefault();

                if (shape != null)
                {
                    shape.X = (int)(vp.Value.X*5);
                    shape.Y = (int)(vp.Value.Y*5);
                }

            }

        }

        #endregion

        #region Status Bar
        private void ToggleProgressBarMain(bool visible)
        {
            if (toolStripProgressBarMain.GetCurrentParent().InvokeRequired)
            {
                toolStripProgressBarMain.GetCurrentParent().Invoke(new MethodInvoker(delegate { toolStripProgressBarMain.Visible = visible; }));
            }
            else
            {
                toolStripProgressBarMain.Visible = visible;
            }
        }
        #endregion

        #region Repository Explorer

        private void toolStripButtonRepositoryExplorerRefresh_Click(object sender, EventArgs e)
        {
            treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher);
        }

        private void toolStripButtonRepositoryExplorerExpand_Click(object sender, EventArgs e)
        {
            treeViewRepositoryExplorer.ExpandAll();
        }

        private void toolStripButtonRepositoryExplorerDiff_Click(object sender, EventArgs e)
        {
            // Git Repo

            if (Repository.IsValid(repoLocalPath))
            {
                try
                {
                    using (var repo = new Repository(repoLocalPath))
                    {
                        richTextBoxOutput.AppendParagraphAutoScroll("=== GIT REPO ===");
                        richTextBoxOutput.AppendParagraphAutoScroll("Diff:");

                        // Diff
                        foreach (var item in repo.RetrieveStatus())
                        {
                            if (item.State == FileStatus.ModifiedInWorkdir)
                            {
                                var patch = repo.Diff.Compare<Patch>(new List<string>() { item.FilePath });
                                richTextBoxOutput.AppendParagraphAutoScroll("~~~~ Patch file ~~~~");

                                StringReader srContent = new StringReader(patch.Content);

                                StringBuilder sbContent = new StringBuilder();

                                string line = string.Empty;
                                while((line = srContent.ReadLine()) != null)
                                {
                                    if(line.StartsWith("+"))
                                    {
                                        sbContent.AppendFormat("\u001b[32m{0}\u001b[0m\n", line);
                                    } else
                                    {
                                        if (line.StartsWith("-"))
                                        {
                                            sbContent.AppendFormat("\u001b[31m{0}\u001b[0m\n", line);
                                        }
                                        else
                                        {
                                            sbContent.AppendLine(line);
                                        }
                                    }
                                }

                                richTextBoxOutput.AppendParagraphAutoScroll(sbContent.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Error: Not a git repository. Clone first.");
            }
        }

        private void toolStripButtonRepositoryExplorerPull_Click(object sender, EventArgs e)
        {
            // Git Repo

            if (Repository.IsValid(repoLocalPath))
            {
                try
                {
                    richTextBoxOutput.AppendParagraphAutoScroll("=== GIT REPO ===");
                    richTextBoxOutput.AppendAutoScroll("Fetching...");
                    string logMessage = "";
                    using (var repo = new Repository(repoLocalPath))
                    {
                        var po = new PullOptions();

                        po.FetchOptions = new FetchOptions();

                        po.FetchOptions.CredentialsProvider = (_url, _user, _cred) => { return gitCredentials; };

                        Signature signature = new Signature(gitCredentials.Username.ToString(), gitCredentials.Username.ToString(), DateTime.Now);

                        Commands.Pull(repo, signature, po);
                    }

                    treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher);

                    richTextBoxOutput.AppendAutoScroll("done");

                    richTextBoxOutput.AppendParagraphAutoScroll(logMessage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Error: Not a git repository. Clone first.");
            }
        }

        private void toolStripButtonRepositoryExplorerCommit_Click(object sender, EventArgs e)
        {
            // Git Repo

            if (Repository.IsValid(repoLocalPath))
            {
                try
                {
                    using (var repo = new Repository(repoLocalPath))
                    {
                        richTextBoxOutput.AppendParagraphAutoScroll("=== GIT REPO ===");
                        richTextBoxOutput.AppendParagraphAutoScroll("Status:");

                        // Status
                        foreach (var item in repo.RetrieveStatus(new StatusOptions()))
                        {

                            StringBuilder sbItem = new StringBuilder();

                            switch (item.State)
                            {
                                case FileStatus.NewInWorkdir:
                                    sbItem.Append("\u001b[32m");
                                    break;
                                case FileStatus.ModifiedInWorkdir:
                                    sbItem.Append("\u001b[33m");
                                    break;
                                case FileStatus.DeletedFromWorkdir:
                                    sbItem.Append("\u001b[31m");
                                    break;
                                case FileStatus.RenamedInWorkdir:
                                    sbItem.Append("\u001b[36m");
                                    break;
                                case FileStatus.Conflicted:
                                    sbItem.Append("\u001b[31m");
                                    break;
                                default:
                                    sbItem.Append("\u001b[37m");
                                    break;
                            }

                            sbItem.Append(item.State.ToString());
                            sbItem.Append(":\u001b[0m  ");
                            sbItem.Append(item.FilePath);

                            richTextBoxOutput.AppendLineAutoScroll(sbItem.ToString());
                        }

                        if (MessageBox.Show("Continue?", "git add -> git commit -> git push", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            // Stage Changes
                            richTextBoxOutput.AppendLineAutoScroll("Staging...");
                            Commands.Stage(repo, "*");

                            if (repo.RetrieveStatus().IsDirty)
                            {
                                // Create Commit
                                richTextBoxOutput.AppendLineAutoScroll("Creating Commit...");
                                Signature author = new Signature(gitCredentials.Username.ToString(), gitCredentials.Username.ToString(), DateTime.Now);
                                Signature committer = author;

                                string commitMessage = "COMMIT " + DateTime.Now.ToLongDateString();

                                richTextBoxOutput.AppendParagraphAutoScroll("\t" + commitMessage + " by: " + gitCredentials.Username.ToString());

                                repo.Commit(commitMessage, author, committer);
                            }
                            else
                            {
                                Branch branch = repo.Branches["master"];

                                richTextBoxOutput.AppendParagraphAutoScroll("Branch " + branch.FriendlyName + " is tracking: " +
                                    branch.IsTracking + ", and is ahead by " + branch.TrackingDetails.AheadBy + " commits.");
                            }

                            // Push
                            richTextBoxOutput.AppendLineAutoScroll("Pushing...");
                            Remote remote = repo.Network.Remotes["origin"];
                            var options = new PushOptions();
                            options.CredentialsProvider = (_url, _user, _cred) => { return gitCredentials; };
                            repo.Network.Push(remote, @"refs/heads/master", options);

                            richTextBoxOutput.AppendLineAutoScroll("=== \u001b[32mPUSH COMPLETE\u001b[0m ===");
                        }
                    }

                    treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                    richTextBoxOutput.AppendLineAutoScroll("=== \u001b[31mERROR: UNABLE TO PUSH\u001b[0m ===");
                }
            }
            else
            {
                MessageBox.Show("Error: Not a git repository. Clone first.");
            }
        }

        private void toolStripButtonRepositoryExplorerPurge_Click(object sender, EventArgs e)
        {            
            if (MessageBox.Show(
                "Purge will delete the local repository and all changes that have not been commited will be lost. This cannot be undone. Are you sure?",
                "Purge Repository",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                RecursiveDelete(new DirectoryInfo(repoLocalPath));

                try
                {
                    richTextBoxOutput.AppendParagraphAutoScroll("Cloning repository...");

                    var co = new CloneOptions();
                    co.CredentialsProvider = (_url, _user, _cred) => { return gitCredentials; };
                    Repository.Clone(remoteRepo, repoLocalPath, co);

                    treeViewRepositoryExplorer.LoadFromGitRepository(repoLocalPath, watcher);

                    richTextBoxOutput.AppendAutoScroll("done");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Clone Failed", MessageBoxButtons.OK);
                    richTextBoxOutput.AppendLineAutoScroll("=== \u001b[31mERROR: UNABLE TO CLONE\u001b[0m ===");
                }
            }
        }

        private void toolStripButtonRepositoryExplorerOpenFolder_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(repoLocalPath);
        }

        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            try
            {
                if (!baseDir.Exists)
                {
                    return;
                }

                foreach (var dir in baseDir.EnumerateDirectories())
                {
                    RecursiveDelete(dir);
                }

                var files = baseDir.GetFiles();
                foreach (var file in files)
                {
                    file.IsReadOnly = false;
                    file.Delete();
                }

                baseDir.Delete();
            }
            catch (Exception ex)
            {
                if(MessageBox.Show("Error deleting directory: " + ex.Message, "Error", MessageBoxButtons.RetryCancel) == DialogResult.Retry)
                {
                    RecursiveDelete(baseDir);
                }
            }
        }

        private void treeViewRepositoryExplorer_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            string file_name = (string)e.Node.Tag;

            if (!string.IsNullOrWhiteSpace(file_name))
            {
                if (!openFileTabs.ContainsKey(file_name))
                {
                    try
                    {
                        //Check if file_name is not a directory
                        if ((File.GetAttributes(file_name) & FileAttributes.Directory) != FileAttributes.Directory)
                        {

                            display.CodeEditor.CodeEditor ce = new display.CodeEditor.CodeEditor();

                            var editor = ce.CreateEditor();

                            ce.igp_topology = igp_topology;
                            ce.mpls_topology = mpls_topology;
                            ce.rtbOutput = richTextBoxOutput;

                            DockContent dockContentEditor = new DockContent();

                            dockContentEditor.Controls.Add(editor);
                            editor.Dock = DockStyle.Fill;

                            dockContentEditor.ShowHint = DockState.Document;
                            dockContentEditor.TabText = Path.GetFileName(file_name);
                            dockContentEditor.Icon = this.Icon;
                            dockContentEditor.Show(dockPanelMain);

                            ce.LoadFile((EasyScintilla.SimpleEditor)editor.Controls["codeEditor"], file_name);

                            dockContentEditor.Tag = file_name;

                            openFileTabs.Add(file_name, dockContentEditor);

                            ce.OnSavePointLeftCallback += () =>
                            {
                                dockContentEditor.TabText += "*";
                            };

                            ce.OnSavePointReachedCallback += () =>
                            {
                                dockContentEditor.TabText = dockContentEditor.TabText.TrimEnd("*".ToCharArray());
                            };

                            ce.OnExecCompletedCallback += (object res) =>
                            {
                                code.OutputProcessor.IntentProcessor(res, mpls_topology, igp_topology, displayNetwork, treeViewTunnels, toolStripProgressBarMain);
                            };

                            if (ce.Error)
                            {
                                dockContentEditor.Close();
                            }
                            else
                            {
                                dockContentEditor.FormClosing += (s, ea) =>
                                {
                                // Check if doc dirty
                                if (dockContentEditor.TabText.Contains("*"))
                                    {
                                        if (MessageBox.Show("The current document has been modified. Close without saving?", "Document", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                                        {
                                            ea.Cancel = true;
                                        }
                                    }
                                };

                                dockContentEditor.FormClosed += (s, ea) =>
                                {
                                    openFileTabs.Remove((string)((Control)s).Tag);
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show("Error: " + ex.Message, "Error");
                    }
                }
                else
                {
                    openFileTabs[file_name].Focus();
                } 
            }
        }

        private void treeViewRepositoryExplorer_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (string.IsNullOrWhiteSpace((string)e.Node.Tag))
            {
                e.CancelEdit = true;
            }
        }

        private void treeViewRepositoryExplorer_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {

            if (e.Node != null)
            {
                string currentFileName = (string)e.Node.Tag;

                try
                {
                    string newFileName = Path.Combine(Path.GetDirectoryName(currentFileName), e.Label);

                    FileAttributes attr = File.GetAttributes(currentFileName);

                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        Directory.Move(currentFileName, newFileName);
                        e.Node.Tag = newFileName;

                    }
                    else
                    {

                        File.Move(currentFileName, newFileName);
                        e.Node.Tag = newFileName;
                    }
                }
                catch (Exception ex)
                {
                    e.Node.Text = Path.GetFileName(currentFileName);
                    MessageBox.Show("Error: " + ex.Message, "Error");
                } 
            }
        }

        #endregion

        #region Result Display Events

        private void treeViewTunnels_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                try
                { propertyGridResult.SelectedObject = e.Node.Tag; }
                catch (Exception) { }

                Render.RestoreLineStyles(displayNetwork);

                if (e.Node.Tag.GetType() == typeof(UI.ShapeTag))
                {
                    var tag = (UI.ShapeTag)e.Node.Tag;

                    if (tag.Object.GetType() == typeof(yggdrasil2.Topology.IGP.Link.Link))
                    {
                        NShapeHelper.HighlightLink(tag, displayNetwork);
                    }
                }

                if (e.Node.Tag.GetType() == typeof(yggdrasil2.Topology.MPLS.LabelSwitchedPath.LabelSwitchedPath))
                {
                    var lsp = (yggdrasil2.Topology.MPLS.LabelSwitchedPath.LabelSwitchedPath)e.Node.Tag;

                    var source = igp_topology.Nodes.Where(n => n.IPv4RouterIdentifier == lsp.IPv4TunnelSenderAddress).SingleOrDefault();
                    var target = igp_topology.Nodes.Where(n => n.IPv4RouterIdentifier == lsp.IPv4TunnelEndpointAddress).SingleOrDefault();

                    if (source != null)
                    {
                        NShapeHelper.HighlightShapeByShapeTag(new UI.ShapeTag(source.IgpRouterIdentifier, source), displayNetwork);
                    }

                    if(target != null)
                    {
                        NShapeHelper.HighlightShapeByShapeTag(new UI.ShapeTag(target.IgpRouterIdentifier, target), displayNetwork);
                    }

                    var links = igp_topology.Links.Where(l => lsp.ComputedExplicitRouteObject.Contains(l.IPv4InterfaceAddress));

                    foreach (var link in links)
                    {
                        NShapeHelper.HighlightLink(new UI.ShapeTag(link.Id, link), displayNetwork);
                    }

                }
            }
        }

        #endregion

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            cq.Disconnect();

            tq_igp.Disconnect();
            tq_mpls.Disconnect();

            pq.Disconnect();
            nq.Disconnect();
        }

        #region Console Log Options

        private void toolStripButtonLogIGPTopologyChanges_CheckedChanged(object sender, EventArgs e)
        {
            logOptions.DebugIGPTopology = ((ToolStripButton)sender).Checked;
        }

        private void toolStripButtonLogMPLSTopologyChanges_CheckedChanged(object sender, EventArgs e)
        {
            logOptions.DebugMPLSTopology = ((ToolStripButton)sender).Checked;
        }
        
        private void toolStripButtonLogPerformanceData_CheckedChanged(object sender, EventArgs e)
        {
            logOptions.DebugPerformanceData = ((ToolStripButton)sender).Checked;
        }

        private void toolStripButtonLogNodePCC_CheckedChanged(object sender, EventArgs e)
        {
            logOptions.DebugNodePCC = ((ToolStripButton)sender).Checked;
        }

        #endregion
        
    }


    #region let's get schwifty
    class GraphAreaExample : GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }

    class DataEdge : EdgeBase<DataVertex>
    {
        /// <summary>
        /// Default constructor. We need to set at least Source and Target properties of the edge.
        /// </summary>
        /// <param name="source">Source vertex data</param>
        /// <param name="target">Target vertex data</param>
        /// <param name="weight">Optional edge weight</param>
        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {
        }
        /// <summary>
        /// Default parameterless constructor (for serialization compatibility)
        /// </summary>
        public DataEdge()
            : base(null, null, 1)
        {
        }

        /// <summary>
        /// Custom string property for example
        /// </summary>
        public string Text { get; set; }

        #region GET members
        public override string ToString()
        {
            return Text;
        }
        #endregion
    }

    public class DataVertex : VertexBase
    {
        /// <summary>
        /// Some string property for example purposes
        /// </summary>
        public string Text { get; set; }

        #region Calculated or static props

        public override string ToString()
        {
            return Text;
        }


        #endregion

        /// <summary>
        /// Default parameterless constructor for this class
        /// (required for YAXLib serialization)
        /// </summary>
        public DataVertex() : this("")
        {
        }

        public DataVertex(string text = "")
        {
            Text = text;
        }
    }

    #endregion

    class LogOptions
    {
        public bool DebugIGPTopology { get; set; }
        public bool DebugMPLSTopology { get; set; }
        public bool DebugPerformanceData { get; set; }
        public bool DebugNodePCC { get; set; }
    }
}

