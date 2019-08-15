namespace shift.ui.architect
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.dockPanelMain = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.menuStripMain = new System.Windows.Forms.MenuStrip();
            this.layoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.automaticToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelRepositoryExplorer = new System.Windows.Forms.Panel();
            this.toolStripRepositoryExplorer = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonRepositoryExplorerRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRepositoryExplorerExpand = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonRepositoryExplorerDiff = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRepositoryExplorerPull = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRepositoryExplorerCommit = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonRepositoryExplorerPurge = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonRepositoryExplorerOpenFolder = new System.Windows.Forms.ToolStripButton();
            this.treeViewRepositoryExplorer = new System.Windows.Forms.TreeView();
            this.imageListRepositoryExplorer = new System.Windows.Forms.ImageList(this.components);
            this.richTextBoxOutput = new System.Windows.Forms.RichTextBox();
            this.panelOutput = new System.Windows.Forms.Panel();
            this.toolStripConsole = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonLogIGPTopologyChanges = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonLogMPLSTopologyChanges = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonLogPerformanceData = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonLogNodePCC = new System.Windows.Forms.ToolStripButton();
            this.panelProperties = new System.Windows.Forms.Panel();
            this.propertyGridProperties = new PropertyGridEx.PropertyGridEx();
            this.panelTunnels = new System.Windows.Forms.Panel();
            this.treeViewTunnels = new System.Windows.Forms.TreeView();
            this.panelResult = new System.Windows.Forms.Panel();
            this.propertyGridResult = new System.Windows.Forms.PropertyGrid();
            this.contextMenuStripRepositoryExplorer = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.statusStripMain = new System.Windows.Forms.StatusStrip();
            this.toolStripProgressBarMain = new System.Windows.Forms.ToolStripProgressBar();
            this.menuStripMain.SuspendLayout();
            this.panelRepositoryExplorer.SuspendLayout();
            this.toolStripRepositoryExplorer.SuspendLayout();
            this.panelOutput.SuspendLayout();
            this.toolStripConsole.SuspendLayout();
            this.panelProperties.SuspendLayout();
            this.panelTunnels.SuspendLayout();
            this.panelResult.SuspendLayout();
            this.statusStripMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // dockPanelMain
            // 
            this.dockPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dockPanelMain.Location = new System.Drawing.Point(0, 24);
            this.dockPanelMain.Name = "dockPanelMain";
            this.dockPanelMain.Size = new System.Drawing.Size(1260, 594);
            this.dockPanelMain.TabIndex = 1;
            // 
            // menuStripMain
            // 
            this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.layoutToolStripMenuItem});
            this.menuStripMain.Location = new System.Drawing.Point(0, 0);
            this.menuStripMain.Name = "menuStripMain";
            this.menuStripMain.Size = new System.Drawing.Size(1260, 24);
            this.menuStripMain.TabIndex = 4;
            this.menuStripMain.Text = "menuStrip1";
            // 
            // layoutToolStripMenuItem
            // 
            this.layoutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.automaticToolStripMenuItem});
            this.layoutToolStripMenuItem.Name = "layoutToolStripMenuItem";
            this.layoutToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            this.layoutToolStripMenuItem.Text = "&Layout";
            // 
            // automaticToolStripMenuItem
            // 
            this.automaticToolStripMenuItem.Name = "automaticToolStripMenuItem";
            this.automaticToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.automaticToolStripMenuItem.Text = "&Automatic";
            this.automaticToolStripMenuItem.Click += new System.EventHandler(this.automaticToolStripMenuItem_Click);
            // 
            // panelRepositoryExplorer
            // 
            this.panelRepositoryExplorer.Controls.Add(this.toolStripRepositoryExplorer);
            this.panelRepositoryExplorer.Controls.Add(this.treeViewRepositoryExplorer);
            this.panelRepositoryExplorer.Location = new System.Drawing.Point(156, 103);
            this.panelRepositoryExplorer.Name = "panelRepositoryExplorer";
            this.panelRepositoryExplorer.Size = new System.Drawing.Size(200, 357);
            this.panelRepositoryExplorer.TabIndex = 6;
            // 
            // toolStripRepositoryExplorer
            // 
            this.toolStripRepositoryExplorer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonRepositoryExplorerRefresh,
            this.toolStripButtonRepositoryExplorerExpand,
            this.toolStripSeparator1,
            this.toolStripButtonRepositoryExplorerDiff,
            this.toolStripButtonRepositoryExplorerPull,
            this.toolStripButtonRepositoryExplorerCommit,
            this.toolStripSeparator3,
            this.toolStripButtonRepositoryExplorerPurge,
            this.toolStripSeparator2,
            this.toolStripButtonRepositoryExplorerOpenFolder});
            this.toolStripRepositoryExplorer.Location = new System.Drawing.Point(0, 0);
            this.toolStripRepositoryExplorer.Name = "toolStripRepositoryExplorer";
            this.toolStripRepositoryExplorer.Size = new System.Drawing.Size(200, 25);
            this.toolStripRepositoryExplorer.TabIndex = 1;
            this.toolStripRepositoryExplorer.Text = "toolStrip1";
            // 
            // toolStripButtonRepositoryExplorerRefresh
            // 
            this.toolStripButtonRepositoryExplorerRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRepositoryExplorerRefresh.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepositoryExplorerRefresh.Image")));
            this.toolStripButtonRepositoryExplorerRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepositoryExplorerRefresh.Name = "toolStripButtonRepositoryExplorerRefresh";
            this.toolStripButtonRepositoryExplorerRefresh.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRepositoryExplorerRefresh.Text = "Local Refresh";
            this.toolStripButtonRepositoryExplorerRefresh.Click += new System.EventHandler(this.toolStripButtonRepositoryExplorerRefresh_Click);
            // 
            // toolStripButtonRepositoryExplorerExpand
            // 
            this.toolStripButtonRepositoryExplorerExpand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRepositoryExplorerExpand.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepositoryExplorerExpand.Image")));
            this.toolStripButtonRepositoryExplorerExpand.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepositoryExplorerExpand.Name = "toolStripButtonRepositoryExplorerExpand";
            this.toolStripButtonRepositoryExplorerExpand.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRepositoryExplorerExpand.Text = "Expand All";
            this.toolStripButtonRepositoryExplorerExpand.Click += new System.EventHandler(this.toolStripButtonRepositoryExplorerExpand_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonRepositoryExplorerDiff
            // 
            this.toolStripButtonRepositoryExplorerDiff.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRepositoryExplorerDiff.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepositoryExplorerDiff.Image")));
            this.toolStripButtonRepositoryExplorerDiff.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepositoryExplorerDiff.Name = "toolStripButtonRepositoryExplorerDiff";
            this.toolStripButtonRepositoryExplorerDiff.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRepositoryExplorerDiff.Text = "Diff";
            this.toolStripButtonRepositoryExplorerDiff.Click += new System.EventHandler(this.toolStripButtonRepositoryExplorerDiff_Click);
            // 
            // toolStripButtonRepositoryExplorerPull
            // 
            this.toolStripButtonRepositoryExplorerPull.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRepositoryExplorerPull.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepositoryExplorerPull.Image")));
            this.toolStripButtonRepositoryExplorerPull.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepositoryExplorerPull.Name = "toolStripButtonRepositoryExplorerPull";
            this.toolStripButtonRepositoryExplorerPull.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRepositoryExplorerPull.Text = "Pull";
            this.toolStripButtonRepositoryExplorerPull.Click += new System.EventHandler(this.toolStripButtonRepositoryExplorerPull_Click);
            // 
            // toolStripButtonRepositoryExplorerCommit
            // 
            this.toolStripButtonRepositoryExplorerCommit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRepositoryExplorerCommit.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepositoryExplorerCommit.Image")));
            this.toolStripButtonRepositoryExplorerCommit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepositoryExplorerCommit.Name = "toolStripButtonRepositoryExplorerCommit";
            this.toolStripButtonRepositoryExplorerCommit.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRepositoryExplorerCommit.Text = "Commit";
            this.toolStripButtonRepositoryExplorerCommit.Click += new System.EventHandler(this.toolStripButtonRepositoryExplorerCommit_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonRepositoryExplorerPurge
            // 
            this.toolStripButtonRepositoryExplorerPurge.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRepositoryExplorerPurge.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepositoryExplorerPurge.Image")));
            this.toolStripButtonRepositoryExplorerPurge.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepositoryExplorerPurge.Name = "toolStripButtonRepositoryExplorerPurge";
            this.toolStripButtonRepositoryExplorerPurge.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRepositoryExplorerPurge.Text = "Purge";
            this.toolStripButtonRepositoryExplorerPurge.Click += new System.EventHandler(this.toolStripButtonRepositoryExplorerPurge_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonRepositoryExplorerOpenFolder
            // 
            this.toolStripButtonRepositoryExplorerOpenFolder.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRepositoryExplorerOpenFolder.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepositoryExplorerOpenFolder.Image")));
            this.toolStripButtonRepositoryExplorerOpenFolder.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepositoryExplorerOpenFolder.Name = "toolStripButtonRepositoryExplorerOpenFolder";
            this.toolStripButtonRepositoryExplorerOpenFolder.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRepositoryExplorerOpenFolder.Text = "Open Folder";
            this.toolStripButtonRepositoryExplorerOpenFolder.Click += new System.EventHandler(this.toolStripButtonRepositoryExplorerOpenFolder_Click);
            // 
            // treeViewRepositoryExplorer
            // 
            this.treeViewRepositoryExplorer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeViewRepositoryExplorer.ImageIndex = 4;
            this.treeViewRepositoryExplorer.ImageList = this.imageListRepositoryExplorer;
            this.treeViewRepositoryExplorer.ItemHeight = 16;
            this.treeViewRepositoryExplorer.LabelEdit = true;
            this.treeViewRepositoryExplorer.Location = new System.Drawing.Point(3, 28);
            this.treeViewRepositoryExplorer.Name = "treeViewRepositoryExplorer";
            this.treeViewRepositoryExplorer.SelectedImageIndex = 4;
            this.treeViewRepositoryExplorer.Size = new System.Drawing.Size(194, 326);
            this.treeViewRepositoryExplorer.TabIndex = 0;
            this.treeViewRepositoryExplorer.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeViewRepositoryExplorer_BeforeLabelEdit);
            this.treeViewRepositoryExplorer.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeViewRepositoryExplorer_AfterLabelEdit);
            this.treeViewRepositoryExplorer.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewRepositoryExplorer_NodeMouseDoubleClick);
            // 
            // imageListRepositoryExplorer
            // 
            this.imageListRepositoryExplorer.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListRepositoryExplorer.ImageStream")));
            this.imageListRepositoryExplorer.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListRepositoryExplorer.Images.SetKeyName(0, "icons8-folder-64.png");
            this.imageListRepositoryExplorer.Images.SetKeyName(1, "icons8-code-file-64.png");
            this.imageListRepositoryExplorer.Images.SetKeyName(2, "icons8-red-file-64.png");
            this.imageListRepositoryExplorer.Images.SetKeyName(3, "icons8-delete-file-64.png");
            this.imageListRepositoryExplorer.Images.SetKeyName(4, "icons8-regular-file-64.png");
            // 
            // richTextBoxOutput
            // 
            this.richTextBoxOutput.BackColor = System.Drawing.SystemColors.Desktop;
            this.richTextBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxOutput.ForeColor = System.Drawing.SystemColors.Highlight;
            this.richTextBoxOutput.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxOutput.Name = "richTextBoxOutput";
            this.richTextBoxOutput.ReadOnly = true;
            this.richTextBoxOutput.Size = new System.Drawing.Size(534, 201);
            this.richTextBoxOutput.TabIndex = 9;
            this.richTextBoxOutput.Text = "";
            // 
            // panelOutput
            // 
            this.panelOutput.Controls.Add(this.toolStripConsole);
            this.panelOutput.Controls.Add(this.richTextBoxOutput);
            this.panelOutput.Location = new System.Drawing.Point(375, 386);
            this.panelOutput.Name = "panelOutput";
            this.panelOutput.Size = new System.Drawing.Size(534, 201);
            this.panelOutput.TabIndex = 10;
            // 
            // toolStripConsole
            // 
            this.toolStripConsole.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonLogIGPTopologyChanges,
            this.toolStripButtonLogMPLSTopologyChanges,
            this.toolStripButtonLogPerformanceData,
            this.toolStripButtonLogNodePCC});
            this.toolStripConsole.Location = new System.Drawing.Point(0, 0);
            this.toolStripConsole.Name = "toolStripConsole";
            this.toolStripConsole.Size = new System.Drawing.Size(534, 25);
            this.toolStripConsole.TabIndex = 10;
            this.toolStripConsole.Text = "toolStrip1";
            // 
            // toolStripButtonLogIGPTopologyChanges
            // 
            this.toolStripButtonLogIGPTopologyChanges.CheckOnClick = true;
            this.toolStripButtonLogIGPTopologyChanges.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLogIGPTopologyChanges.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLogIGPTopologyChanges.Image")));
            this.toolStripButtonLogIGPTopologyChanges.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLogIGPTopologyChanges.Name = "toolStripButtonLogIGPTopologyChanges";
            this.toolStripButtonLogIGPTopologyChanges.Size = new System.Drawing.Size(82, 22);
            this.toolStripButtonLogIGPTopologyChanges.Text = "IGP Topology";
            this.toolStripButtonLogIGPTopologyChanges.CheckedChanged += new System.EventHandler(this.toolStripButtonLogIGPTopologyChanges_CheckedChanged);
            // 
            // toolStripButtonLogMPLSTopologyChanges
            // 
            this.toolStripButtonLogMPLSTopologyChanges.CheckOnClick = true;
            this.toolStripButtonLogMPLSTopologyChanges.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLogMPLSTopologyChanges.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLogMPLSTopologyChanges.Image")));
            this.toolStripButtonLogMPLSTopologyChanges.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLogMPLSTopologyChanges.Name = "toolStripButtonLogMPLSTopologyChanges";
            this.toolStripButtonLogMPLSTopologyChanges.Size = new System.Drawing.Size(94, 22);
            this.toolStripButtonLogMPLSTopologyChanges.Text = "MPLS Topology";
            this.toolStripButtonLogMPLSTopologyChanges.CheckedChanged += new System.EventHandler(this.toolStripButtonLogMPLSTopologyChanges_CheckedChanged);
            // 
            // toolStripButtonLogPerformanceData
            // 
            this.toolStripButtonLogPerformanceData.CheckOnClick = true;
            this.toolStripButtonLogPerformanceData.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLogPerformanceData.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLogPerformanceData.Image")));
            this.toolStripButtonLogPerformanceData.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLogPerformanceData.Name = "toolStripButtonLogPerformanceData";
            this.toolStripButtonLogPerformanceData.Size = new System.Drawing.Size(79, 22);
            this.toolStripButtonLogPerformanceData.Text = "Performance";
            this.toolStripButtonLogPerformanceData.CheckedChanged += new System.EventHandler(this.toolStripButtonLogPerformanceData_CheckedChanged);
            // 
            // toolStripButtonLogNodePCC
            // 
            this.toolStripButtonLogNodePCC.CheckOnClick = true;
            this.toolStripButtonLogNodePCC.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLogNodePCC.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLogNodePCC.Image")));
            this.toolStripButtonLogNodePCC.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLogNodePCC.Name = "toolStripButtonLogNodePCC";
            this.toolStripButtonLogNodePCC.Size = new System.Drawing.Size(66, 22);
            this.toolStripButtonLogNodePCC.Text = "Node PCC";
            this.toolStripButtonLogNodePCC.CheckedChanged += new System.EventHandler(this.toolStripButtonLogNodePCC_CheckedChanged);
            // 
            // panelProperties
            // 
            this.panelProperties.Controls.Add(this.propertyGridProperties);
            this.panelProperties.Location = new System.Drawing.Point(962, 55);
            this.panelProperties.Name = "panelProperties";
            this.panelProperties.Size = new System.Drawing.Size(230, 275);
            this.panelProperties.TabIndex = 11;
            // 
            // propertyGridProperties
            // 
            // 
            // 
            // 
            this.propertyGridProperties.DocCommentDescription.AccessibleName = "";
            this.propertyGridProperties.DocCommentDescription.AutoEllipsis = true;
            this.propertyGridProperties.DocCommentDescription.Cursor = System.Windows.Forms.Cursors.Default;
            this.propertyGridProperties.DocCommentDescription.Location = new System.Drawing.Point(3, 18);
            this.propertyGridProperties.DocCommentDescription.Name = "";
            this.propertyGridProperties.DocCommentDescription.Size = new System.Drawing.Size(224, 37);
            this.propertyGridProperties.DocCommentDescription.TabIndex = 1;
            this.propertyGridProperties.DocCommentImage = null;
            // 
            // 
            // 
            this.propertyGridProperties.DocCommentTitle.Cursor = System.Windows.Forms.Cursors.Default;
            this.propertyGridProperties.DocCommentTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.propertyGridProperties.DocCommentTitle.Location = new System.Drawing.Point(3, 3);
            this.propertyGridProperties.DocCommentTitle.Name = "";
            this.propertyGridProperties.DocCommentTitle.Size = new System.Drawing.Size(224, 15);
            this.propertyGridProperties.DocCommentTitle.TabIndex = 0;
            this.propertyGridProperties.DocCommentTitle.UseMnemonic = false;
            this.propertyGridProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridProperties.Location = new System.Drawing.Point(0, 0);
            this.propertyGridProperties.Name = "propertyGridProperties";
            this.propertyGridProperties.SelectedObject = ((object)(resources.GetObject("propertyGridProperties.SelectedObject")));
            this.propertyGridProperties.ShowCustomProperties = true;
            this.propertyGridProperties.Size = new System.Drawing.Size(230, 275);
            this.propertyGridProperties.TabIndex = 0;
            this.propertyGridProperties.ToolbarVisible = false;
            // 
            // 
            // 
            this.propertyGridProperties.ToolStrip.AccessibleName = "ToolBar";
            this.propertyGridProperties.ToolStrip.AccessibleRole = System.Windows.Forms.AccessibleRole.ToolBar;
            this.propertyGridProperties.ToolStrip.AllowMerge = false;
            this.propertyGridProperties.ToolStrip.AutoSize = false;
            this.propertyGridProperties.ToolStrip.CanOverflow = false;
            this.propertyGridProperties.ToolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.propertyGridProperties.ToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.propertyGridProperties.ToolStrip.Location = new System.Drawing.Point(0, 1);
            this.propertyGridProperties.ToolStrip.Name = "";
            this.propertyGridProperties.ToolStrip.Padding = new System.Windows.Forms.Padding(2, 0, 1, 0);
            this.propertyGridProperties.ToolStrip.Size = new System.Drawing.Size(230, 25);
            this.propertyGridProperties.ToolStrip.TabIndex = 1;
            this.propertyGridProperties.ToolStrip.TabStop = true;
            this.propertyGridProperties.ToolStrip.Text = "PropertyGridToolBar";
            this.propertyGridProperties.ToolStrip.Visible = false;
            // 
            // panelTunnels
            // 
            this.panelTunnels.Controls.Add(this.treeViewTunnels);
            this.panelTunnels.Location = new System.Drawing.Point(580, 73);
            this.panelTunnels.Name = "panelTunnels";
            this.panelTunnels.Size = new System.Drawing.Size(225, 257);
            this.panelTunnels.TabIndex = 12;
            // 
            // treeViewTunnels
            // 
            this.treeViewTunnels.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewTunnels.Location = new System.Drawing.Point(0, 0);
            this.treeViewTunnels.Name = "treeViewTunnels";
            this.treeViewTunnels.Size = new System.Drawing.Size(225, 257);
            this.treeViewTunnels.TabIndex = 0;
            this.treeViewTunnels.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewTunnels_NodeMouseClick);
            // 
            // panelResult
            // 
            this.panelResult.Controls.Add(this.propertyGridResult);
            this.panelResult.Location = new System.Drawing.Point(962, 367);
            this.panelResult.Name = "panelResult";
            this.panelResult.Size = new System.Drawing.Size(230, 220);
            this.panelResult.TabIndex = 13;
            // 
            // propertyGridResult
            // 
            this.propertyGridResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridResult.Enabled = false;
            this.propertyGridResult.Location = new System.Drawing.Point(0, 0);
            this.propertyGridResult.Name = "propertyGridResult";
            this.propertyGridResult.Size = new System.Drawing.Size(230, 220);
            this.propertyGridResult.TabIndex = 0;
            // 
            // contextMenuStripRepositoryExplorer
            // 
            this.contextMenuStripRepositoryExplorer.Name = "contextMenuStripRepositoryExplorer";
            this.contextMenuStripRepositoryExplorer.Size = new System.Drawing.Size(61, 4);
            // 
            // statusStripMain
            // 
            this.statusStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBarMain});
            this.statusStripMain.Location = new System.Drawing.Point(0, 596);
            this.statusStripMain.Name = "statusStripMain";
            this.statusStripMain.Size = new System.Drawing.Size(1260, 22);
            this.statusStripMain.TabIndex = 16;
            this.statusStripMain.Text = "statusStrip1";
            // 
            // toolStripProgressBarMain
            // 
            this.toolStripProgressBarMain.Name = "toolStripProgressBarMain";
            this.toolStripProgressBarMain.Size = new System.Drawing.Size(100, 16);
            this.toolStripProgressBarMain.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1260, 618);
            this.Controls.Add(this.statusStripMain);
            this.Controls.Add(this.panelResult);
            this.Controls.Add(this.panelTunnels);
            this.Controls.Add(this.panelProperties);
            this.Controls.Add(this.panelOutput);
            this.Controls.Add(this.panelRepositoryExplorer);
            this.Controls.Add(this.dockPanelMain);
            this.Controls.Add(this.menuStripMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStripMain;
            this.Name = "FormMain";
            this.Text = "SHIFT Architect";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.panelRepositoryExplorer.ResumeLayout(false);
            this.panelRepositoryExplorer.PerformLayout();
            this.toolStripRepositoryExplorer.ResumeLayout(false);
            this.toolStripRepositoryExplorer.PerformLayout();
            this.panelOutput.ResumeLayout(false);
            this.panelOutput.PerformLayout();
            this.toolStripConsole.ResumeLayout(false);
            this.toolStripConsole.PerformLayout();
            this.panelProperties.ResumeLayout(false);
            this.panelTunnels.ResumeLayout(false);
            this.panelResult.ResumeLayout(false);
            this.statusStripMain.ResumeLayout(false);
            this.statusStripMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private WeifenLuo.WinFormsUI.Docking.DockPanel dockPanelMain;
        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ToolStripMenuItem layoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem automaticToolStripMenuItem;
        private System.Windows.Forms.Panel panelRepositoryExplorer;
        private System.Windows.Forms.TreeView treeViewRepositoryExplorer;
        private System.Windows.Forms.ImageList imageListRepositoryExplorer;
        private System.Windows.Forms.ToolStrip toolStripRepositoryExplorer;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepositoryExplorerRefresh;
        private System.Windows.Forms.RichTextBox richTextBoxOutput;
        private System.Windows.Forms.Panel panelOutput;
        private System.Windows.Forms.Panel panelProperties;
        private System.Windows.Forms.Panel panelTunnels;
        private System.Windows.Forms.TreeView treeViewTunnels;
        private System.Windows.Forms.Panel panelResult;
        private System.Windows.Forms.PropertyGrid propertyGridResult;
        private PropertyGridEx.PropertyGridEx propertyGridProperties;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepositoryExplorerCommit;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepositoryExplorerPurge;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepositoryExplorerOpenFolder;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepositoryExplorerPull;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepositoryExplorerDiff;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripRepositoryExplorer;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepositoryExplorerExpand;
        private System.Windows.Forms.ToolStrip toolStripConsole;
        private System.Windows.Forms.ToolStripButton toolStripButtonLogIGPTopologyChanges;
        private System.Windows.Forms.ToolStripButton toolStripButtonLogMPLSTopologyChanges;
        private System.Windows.Forms.ToolStripButton toolStripButtonLogPerformanceData;
        private System.Windows.Forms.ToolStripButton toolStripButtonLogNodePCC;
        private System.Windows.Forms.StatusStrip statusStripMain;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBarMain;
    }
}

