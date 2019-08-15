using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using LibGit2Sharp;

namespace shift.ui.Architect.display.controls
{
    static class TreeViewExtensions
    {
        // Initialize the TreeView from a directory,
        // its subdirectories, and their files.
        public static void LoadFromGitRepository(this TreeView trv, string directory, FileSystemWatcher watcher)
        {
            string selected = string.Empty;
            if(trv.SelectedNode != null)
            {
                selected = trv.SelectedNode.Text;
            }

            trv.Nodes.Clear();

            if (Repository.IsValid(directory))
            {
                trv.Tag = directory;

                DirectoryInfo dir_info = new DirectoryInfo(directory);
                Repository repo = new Repository(directory);

                AddDirectoryNodes(trv, dir_info, repo, null, selected, watcher);
            }
            else
            {
                MessageBox.Show("Error: Not a valid repository. Clone first");
            }
        }

        // Add this directory's node and sub-nodes.
        public static void AddDirectoryNodes(TreeView trv,
            DirectoryInfo dir_info, Repository repo, TreeNode parent, string selected, FileSystemWatcher watcher)
        {
            // Hide directories and files starting with '.'
            if (!dir_info.Name.StartsWith("."))
            {
                // Add the directory's node.
                TreeNode dir_node;
                if (parent == null)
                {
                    dir_node = trv.Nodes.Add(dir_info.Name);
                }
                else
                {
                    dir_node = parent.Nodes.Add(dir_info.Name);
                    dir_node.Tag = dir_info.FullName;
                }

                // Add the folder image.
                dir_node.ImageIndex = 0;
                dir_node.SelectedImageIndex = 0;

                // Add Directory Menu
                ContextMenuStrip dirMenu = new ContextMenuStrip();
                ToolStripMenuItem cmdNewFile = new ToolStripMenuItem("New");
                cmdNewFile.Tag = dir_node;
                cmdNewFile.Click += (s, ea) => {
                    try
                    {
                        FileInfo newFileInfo = new FileInfo(Path.Combine(dir_info.FullName, "new_intent.shift"));

                        int newFileIndex = 1;

                        while(newFileInfo.Exists)
                        {
                            newFileInfo = new FileInfo(Path.Combine(dir_info.FullName, "new_intent(" + newFileIndex +").shift"));
                            newFileIndex++;
                        }

                        watcher.EnableRaisingEvents = false;

                        newFileInfo.Create().Close();
                        newFileInfo.AppendText().Close();

                        TreeNode file_node = ((TreeNode)((ToolStripMenuItem)s).Tag).Nodes.Add(newFileInfo.Name);
                        file_node.Tag = newFileInfo.FullName;

                        SetFileNodeRepoStatus(repo, newFileInfo, file_node);

                        trv.SelectedNode = file_node;

                        watcher.EnableRaisingEvents = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                    }
                };
                dirMenu.Items.Add(cmdNewFile);

                dir_node.ContextMenuStrip = dirMenu;

                // Add subdirectories.
                foreach (DirectoryInfo subdir in dir_info.GetDirectories())
                {
                    AddDirectoryNodes(trv, subdir, repo, dir_node, selected, watcher);
                }

                // Add file nodes.
                foreach (FileInfo file_info in dir_info.GetFiles())
                {
                    TreeNode file_node = dir_node.Nodes.Add(file_info.Name);

                    file_node.Tag = file_info.FullName;

                    SetFileNodeRepoStatus(repo, file_info, file_node);

                    // Add File Menu
                    ContextMenuStrip fileMenu = new ContextMenuStrip();
                    ToolStripMenuItem cmdDeleteFile = new ToolStripMenuItem("Delete");
                    cmdDeleteFile.Tag = file_node;
                    cmdDeleteFile.Click += (s, ea) => {
                        try
                        {
                            FileInfo fileInfo = new FileInfo((string)((TreeNode)((ToolStripMenuItem)s).Tag).Tag);
                            if (MessageBox.Show("Delete file " + fileInfo.Name + "?", "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                fileInfo.Delete(); 
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error");
                        }
                    };

                    fileMenu.Items.Add(cmdDeleteFile);

                    file_node.ContextMenuStrip = fileMenu;


                    if (file_info.Name == selected)
                    {
                        var p = file_node.Parent;

                        while(p != null)
                        {
                            p.Expand();
                            p = p.Parent;
                        }

                        trv.SelectedNode = file_node;
                    }
                } 
            }
        }

        public static void SetFileNodeRepoStatus(Repository repo, FileInfo file_info, TreeNode file_node)
        {
            FileStatus file_status = repo.RetrieveStatus(file_info.FullName);

            // TODO: Complete Icon Images
            switch (file_status)
            {
                case FileStatus.Nonexistent:
                    break;
                case FileStatus.Unaltered:
                    file_node.ImageIndex = 1;
                    file_node.SelectedImageIndex = 1;
                    break;
                case FileStatus.NewInIndex:
                    break;
                case FileStatus.ModifiedInIndex:
                    break;
                case FileStatus.DeletedFromIndex:
                    break;
                case FileStatus.RenamedInIndex:
                    break;
                case FileStatus.TypeChangeInIndex:
                    break;
                case FileStatus.NewInWorkdir:
                    file_node.ImageIndex = 2;
                    file_node.SelectedImageIndex = 2;
                    break;
                case FileStatus.ModifiedInWorkdir:
                    file_node.ImageIndex = 3;
                    file_node.SelectedImageIndex = 3;
                    break;
                case FileStatus.DeletedFromWorkdir:
                    break;
                case FileStatus.TypeChangeInWorkdir:
                    break;
                case FileStatus.RenamedInWorkdir:
                    break;
                case FileStatus.Unreadable:
                    break;
                case FileStatus.Ignored:
                    break;
                case FileStatus.Conflicted:
                    break;
                default:
                    break;
            }
        }
    }
}
