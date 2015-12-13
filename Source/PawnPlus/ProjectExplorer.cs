﻿using PawnPlus.CodeEditor;
using PawnPlus.Core;
using PawnPlus.Project;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace PawnPlus
{
    public partial class ProjectExplorer : DockContent
    {
        private ContextMenu directoryMenu;
        private ContextMenu fileMenu;
        private ContextMenu rootMenu;

        public ProjectExplorer()
        {
            InitializeComponent();
        }

        private void ProjectExplorer_Load(object sender, EventArgs e)
        {
            ImageList imageList = new ImageList();
            imageList.ColorDepth = ColorDepth.Depth32Bit;

            imageList.Images.Add(Properties.Resources.application_32xLG);
            imageList.Images.Add(Properties.Resources.folder_Closed_32xLG);
            imageList.Images.Add(Properties.Resources.folder_Open_32xLG);
            imageList.Images.Add(Properties.Resources.FileGroup_10135_32x);
            imageList.Images.Add(Properties.Resources.gear_32xLG);

            this.projectFiles.ImageList = imageList;
            this.projectFiles.LabelEdit = true;
            this.projectFiles.TreeViewNodeSorter = new TreeNodeSorter();

            this.projectFiles.AfterLabelEdit += projectFiles_AfterLabelEdit;

            List<MenuItem> menuItems = new List<MenuItem>();

            // Add items for file context menu.
            menuItems.Add(new MenuItem("Delete", this.contextMenuDelete_Click));
            menuItems.Add(new MenuItem("Rename", this.contextMenuRename_Click));
            this.fileMenu = new ContextMenu(menuItems.ToArray());

            menuItems.Clear();

            // Add items for folder context menu.
            menuItems.Add(new MenuItem("Add"));

            // Create sub-items for 'Add' menu.
            menuItems[0].MenuItems.Add(new MenuItem("New File", this.contextMenuCreateFile_Click));
            menuItems[0].MenuItems.Add(new MenuItem("New Folder", this.contextMenuCreateFolder_Click));

            menuItems.Add(new MenuItem("Delete", this.contextMenuDelete_Click));
            menuItems.Add(new MenuItem("Rename", this.contextMenuRename_Click));
            this.directoryMenu = new ContextMenu(menuItems.ToArray());

            menuItems.Clear();

            // Add items for root project context menu.
            menuItems.Add(new MenuItem("Add"));

            // Create sub-items for 'Add' menu.
            menuItems[0].MenuItems.Add(new MenuItem("New File", this.contextMenuCreateFile_Click));
            menuItems[0].MenuItems.Add(new MenuItem("New Folder", this.contextMenuCreateFolder_Click));

            menuItems.Add(new MenuItem("Rename", this.contextMenuRename_Click));
            this.rootMenu = new ContextMenu(menuItems.ToArray());

            ProjectManager.Construct(this.projectFiles);
        }

        private void projectFiles_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level > 0)
            {
                e.Node.ImageIndex = 1;
                e.Node.SelectedImageIndex = 1;
            }
        }

        private void projectFiles_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level > 0)
            {
                e.Node.ImageIndex = 2;
                e.Node.SelectedImageIndex = 2;
            }
        }

        private void projectFiles_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label == null)
            {
                return;
            }
            if (e.Label.Length > 0)
            {
                if (e.Label.IndexOfAny(new char[] { '@', ',', '!' }) == -1)
                {
                    string oldPath = Path.Combine(ProjectManager.Path, e.Node.Text);
                    string newPath = Path.Combine(ProjectManager.Path, e.Label);

                    if ((TreeNodeType)e.Node.Tag == TreeNodeType.Directory)
                    {
                        TreeNodeHelper.ChangeKeys(e.Node.Nodes, oldPath, newPath);
                    }
                    else
                    {
                        TreeNodeHelper.ChangeKey(e.Node, newPath);
                    }

                    // Change directory's name.
                    Directory.Move(oldPath, newPath);

                    e.Node.EndEdit(false);
                }
                else
                {
                    e.CancelEdit = true;
                    // TODO: Show message for invalid characters.
                    e.Node.BeginEdit();
                }
            }
            else
            {
                e.CancelEdit = true;
                // TODO: Show message for empty name.
                e.Node.BeginEdit();
            }
        }

        private void projectFiles_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this.projectFiles.SelectedNode.Level == 0)
            {
                this.projectFiles.SelectedNode.NodeFont = new Font(this.projectFiles.Font, FontStyle.Bold);
                this.projectFiles.SelectedNode.Text = this.projectFiles.SelectedNode.Text;
            }
        }

        private void projectFiles_DoubleClick(object sender, EventArgs e)
        {
            if (File.Exists(this.projectFiles.SelectedNode.Name) == true)
            {
                CEManager.Open(this.projectFiles.SelectedNode.Name, true);
            }

            CEManager.SetActiveDocument(this.projectFiles.SelectedNode.Name, true);
        }

        private void projectFiles_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.projectFiles.Nodes.Count > 0 && e.Button == MouseButtons.Right)
            {
                TreeNode treeNode = this.projectFiles.GetNodeAt(e.Location);

                if (treeNode != null)
                {
                    TreeNode oldTreeNode = this.projectFiles.SelectedNode;
                    this.projectFiles.SelectedNode = treeNode;

                    switch ((TreeNodeType)treeNode.Tag)
                    {
                        case TreeNodeType.Directory:
                            {
                                this.directoryMenu.Show(this.projectFiles, e.Location);
                                break;
                            }
                        case TreeNodeType.File:
                            {
                                this.fileMenu.Show(this.projectFiles, e.Location);
                                break;
                            }
                        case TreeNodeType.Root:
                            {
                                this.rootMenu.Show(this.projectFiles, e.Location);
                                break;
                            }
                    }
                }
            }
        }

        private void contextMenuCreateFile_Click(object sender, EventArgs e)
        {
            // Remove project name from TreeView path.
            string path = this.projectFiles.SelectedNode.FullPath.Remove(0, this.projectFiles.SelectedNode.FullPath.Length > ProjectManager.Name.Length ? ProjectManager.Name.Length + 1 : ProjectManager.Name.Length);
            path = Path.Combine(ProjectManager.Path, path);

            NewForm newForm = new NewForm(NewFormType.File, path);
            newForm.ShowDialog(this);
        }

        private void contextMenuCreateFolder_Click(object sender, EventArgs e)
        {
            // Remove project name from TreeView path.
            string path = this.projectFiles.SelectedNode.FullPath.Remove(0, this.projectFiles.SelectedNode.FullPath.Length > ProjectManager.Name.Length ? ProjectManager.Name.Length + 1 : ProjectManager.Name.Length);
            path = Path.Combine(ProjectManager.Path, path, "New Folder");

            int count = 0;

            // While we have a directory with this name increment "count" to add a suffix to name.
            while (Directory.Exists(path))
            {
                count++;
                path = string.Format("{0} ({1})", path.Replace(string.Format(" ({0})", count - 1), ""), count);
            }

            DirectoryInfo directoryInfo = Directory.CreateDirectory(path);

            ProjectManager.Add(TreeNodeType.Directory, path);
        }

        private void contextMenuDelete_Click(object sender, EventArgs e)
        {
            // Remove project name from TreeView path.
            string path = this.projectFiles.SelectedNode.FullPath.Remove(0, ProjectManager.Name.Length + 1);
            path = Path.Combine(ProjectManager.Path, path);

            FileAttributes fileAttributes = File.GetAttributes(path);

            if (fileAttributes.HasFlag(FileAttributes.Directory) == true)
            {
                // Check if our directory exists.
                if (Directory.Exists(path) == true)
                {
                    Directory.Delete(path, true);
                }
            }
            else
            {
                // Check if our file exists.
                if (File.Exists(path) == true)
                {
                    File.Delete(path);
                }
            }

            this.projectFiles.SelectedNode.Remove();
        }

        private void contextMenuRename_Click(object sender, EventArgs e)
        {
            if (this.projectFiles.SelectedNode.IsEditing == false)
            {
                this.projectFiles.SelectedNode.BeginEdit();
            }
        }
    }
}
