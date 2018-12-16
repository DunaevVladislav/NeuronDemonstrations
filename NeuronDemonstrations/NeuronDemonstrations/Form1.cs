using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuronDemonstrations
{
    public partial class Form1 : Form
    {
        public string imageRootDir = "Images";

        private Dictionary<TreeNode, MyTreeNode> treeNodeDict = new Dictionary<TreeNode, MyTreeNode>();

        private HashSet<string> directories = new HashSet<string>();

        private HashSet<string> files = new HashSet<string>();

        private MyTreeNode selectedNode;

        private string PythonPath = @"D:\Python36\python.exe";

        private string TFP = @"D:\tensorflow-for-poets-2";

        private void InitialTree()
        {
            if (!Directory.Exists(imageRootDir)) Directory.CreateDirectory(imageRootDir);
            foreach (var dir in Directory.GetDirectories(imageRootDir))
            {
                string dirName = Path.GetFileName(dir);
                directories.Add(dirName.ToLower());
                TreeNode treeNode = new TreeNode(dirName);
                treeNodeDict.Add(treeNode, new MyTreeNode(treeNode, dir, 0));
                foreach (var file in Directory.GetFiles(dir))
                {
                    files.Add(Path.GetFileName(file).ToLower());
                    TreeNode subNode = new TreeNode(Path.GetFileNameWithoutExtension(file));
                    treeNode.Nodes.Add(subNode);
                    treeNodeDict.Add(subNode, new MyTreeNode(subNode, file, 1));
                }
                treeView1.Nodes.Add(treeNode);
            }
            Refresh();
        }

        public void CopyTree()
        {
            string tempDir = "temp_Image";
            Directory.CreateDirectory(tempDir);
            foreach (TreeNode treeNode in treeView1.Nodes)
            {
                string dirName = Path.Combine(tempDir, treeNode.Text);
                Directory.CreateDirectory(dirName);
                foreach (TreeNode subNode in treeNode.Nodes)
                {
                    string path = treeNodeDict[subNode].Path;
                    string outPath = Path.Combine(tempDir, treeNode.Text, Path.GetFileName(path));
                    File.Copy(path, outPath);
                }
            }
            Directory.Delete(imageRootDir, true);
            Directory.Move(tempDir, imageRootDir);
        }

        private void CheckButton()
        {
            if (selectedNode == null) button1.Enabled = button3.Enabled = false;
            else
            {
                button3.Enabled = true;
                button1.Enabled = selectedNode.Lvl == 0;
            }
        }

        private void DisplayImage()
        {
            if (selectedNode == null || selectedNode.Lvl == 0)
            {
                pictureBox1.Image = null;
                return;
            }
            pictureBox1.ImageLocation = selectedNode.Path;
        }

        public Form1()
        {
            InitializeComponent();
            InitialTree();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                selectedNode = treeNodeDict[e.Node];
            }
            catch (Exception)
            {
                selectedNode = null;
            }
            CheckButton();
            DisplayImage();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in openFileDialog1.FileNames)
                {
                    file.ToLower();
                    if (files.Contains(Path.GetFileName(file).ToLower()))
                    {
                        MessageBox.Show("Файл с именем " + Path.GetFileName(file) + " уже есть в этой категории");
                        continue;
                    }
                    files.Add(Path.GetFileName(file).ToLower());
                    TreeNode subNode = new TreeNode(Path.GetFileNameWithoutExtension(file));
                    selectedNode.TreeNode.Nodes.Add(subNode);
                    treeNodeDict.Add(subNode, new MyTreeNode(subNode, file, 1));
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string dirName = textBox1.Text;
            directories.Add(dirName.ToLower());
            TreeNode treeNode = new TreeNode(dirName);
            treeNodeDict.Add(treeNode, new MyTreeNode(treeNode, Path.Combine(imageRootDir, dirName), 0));
            treeView1.Nodes.Add(treeNode);
            textBox1.Text = "";
            button2.Enabled = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (selectedNode.Lvl == 0)
            {
                treeView1.Nodes.Remove(selectedNode.TreeNode);
                directories.Remove(Path.GetFileName(selectedNode.Path).ToLower());
            }
            else
            {
                selectedNode.TreeNode.Parent.Nodes.Remove(selectedNode.TreeNode);
                directories.Remove(Path.GetFileName(selectedNode.Path).ToLower());
            }
            treeNodeDict.Remove(selectedNode.TreeNode);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CopyTree();
            string link = Path.Combine(TFP, "tf_files", imageRootDir);
            string target = Path.Combine(Environment.CurrentDirectory, imageRootDir);

            Process proc = new Process();
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = "/c mklink /D " + link + " " + target;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string text = textBox1.Text;
            button2.Enabled = !(directories.Contains(text.ToLower())
                || Path.GetInvalidFileNameChars().Any(o => text.Contains(o)));
        }
    }
}
