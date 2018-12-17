using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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

        private FastProcess fastProcess;

        private string DefaultCpImage = "temp";

        private string CpImage = "";

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
            try
            {
                Directory.CreateDirectory(tempDir);
            }
            catch (Exception) { }
            foreach (TreeNode treeNode in treeView1.Nodes)
            {
                string dirName = Path.Combine(tempDir, treeNode.Text);
                try
                {
                    Directory.CreateDirectory(dirName);
                }
                catch (Exception) { }
                foreach (TreeNode subNode in treeNode.Nodes)
                {
                    string path = treeNodeDict[subNode].Path;
                    string outPath = Path.Combine(tempDir, treeNode.Text, Path.GetFileName(path));
                    try
                    {
                        File.Copy(path, outPath);
                    }
                    catch (Exception) { }
                }
            }
            try
            {
                Directory.Delete(imageRootDir, true);
            }
            catch (Exception) { }
            try
            {
                Directory.Move(tempDir, imageRootDir);
            }
            catch (Exception) { }
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
            fastProcess = new FastProcess(richTextBox1);
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
                foreach(TreeNode subNode in selectedNode.TreeNode.Nodes)
                {
                    string fileName = Path.GetFileName(treeNodeDict[subNode].Path);
                    files.Remove(fileName.ToLower());
                }
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
            var copyTask = Task.Run(() => CopyTree());
            string tf_files = Path.Combine(TFP, "tf_files");
            string link = Path.Combine(tf_files, imageRootDir);
            string target = Path.Combine(Environment.CurrentDirectory, imageRootDir);
            fastProcess.Run("cmd.exe", "/c mklink /D " + link + " " + target, null, true);

            try
            {
                Directory.Delete(Path.Combine(tf_files, "bottlenecks"), true);
            }
            catch (Exception) { };
            try
            {
                File.Delete(Path.Combine(tf_files, "retrained_graph.pb"));
            }
            catch (Exception) { };
            try
            {
                File.Delete(Path.Combine(tf_files, "retrained_labels.txt"));
            }
            catch (Exception) { };

            copyTask.Wait();

            string argsTFPRun =
                PythonPath + " " +
                Path.Combine("scripts", "retrain.py") + " " +
                "--bottleneck_dir=" + Path.Combine("tf_files", "bottlenecks") + " " +
                "--how_many_training_steps=" + numericUpDown1.Value.ToString() + " " +
                "--model_dir=" + Path.Combine("tf_files", "models") + " " +
                "--output_graph=" + Path.Combine("tf_files", "retrained_graph.pb") + " " +
                "--output_labels=" + Path.Combine("tf_files", "retrained_labels.txt") + " " +
                "--image_dir=" + Path.Combine("tf_files", imageRootDir);
            fastProcess.Run("cmd.exe", "/c " + argsTFPRun, TFP);
            recogintion = false;
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string text = textBox1.Text;
            button2.Enabled = !(directories.Contains(text.ToLower())
                || Path.GetInvalidFileNameChars().Any(o => text.Contains(o))
                || text.Any(o => o >='а'&& o <= 'я')
                );
        }

        private void displayImage2()
        {
            if (string.IsNullOrEmpty(CpImage))
            {
                pictureBox1.Image = null;
                button7.Enabled = false;
                return;
            }
            try
            {
                pictureBox2.ImageLocation = CpImage;
                button7.Enabled = true;
            }
            catch (Exception)
            {
                button7.Enabled = false;
                pictureBox1.Image = null;
                CpImage = null;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                var file = openFileDialog2.FileName;
                if (!string.IsNullOrEmpty(CpImage) && File.Exists(CpImage)) File.Delete(CpImage);
                CpImage = Path.GetFileName(file);
                if (File.Exists(CpImage)) File.Delete(CpImage);
                File.Copy(file, Path.Combine(Environment.CurrentDirectory, CpImage));
                displayImage2();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(CpImage) && File.Exists(CpImage)) File.Delete(CpImage);

            string url = textBox2.Text;
            int pos = url.Length - 1;
            while (pos >= 0 && url[pos] != '.') pos--;
            if (pos < 0)
            {
                CpImage = null;
                displayImage2();
                return;
            }
            string expan = url.Substring(pos).ToLower();
            if (expan != ".jpg" && expan != ".png" && expan != "jpeg")
            {
                CpImage = null;
                displayImage2();
                return;
            }
            CpImage = DefaultCpImage + expan;
            if (File.Exists(CpImage)) File.Delete(CpImage);
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.DownloadFile(new Uri(url), CpImage);
                }
                catch (Exception)
                {
                    if (File.Exists(CpImage)) File.Delete(CpImage);
                    CpImage = null;
                };
            }
            displayImage2();
        }

        bool recogintion = false;
        private void button7_Click(object sender, EventArgs e)
        {
            recogintion = true;
            if (string.IsNullOrEmpty(CpImage)) return;

            string argsTFPRun =
                PythonPath + " " +
                Path.Combine("scripts", "label_image.py") + " " +
                "--graph=" + Path.Combine("tf_files", "retrained_graph.pb") + " " +
                "--image=" + Path.Combine(Environment.CurrentDirectory, CpImage);
            fastProcess.Run("cmd.exe", "/c " + argsTFPRun, TFP);
            richTextBox2.Clear();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!recogintion) return;
            var str = richTextBox1.Lines.Last();
            string pat = "(score=";
            int pos = str.IndexOf(pat);
            if (pos == -1) return;
            bool wasEmpty = richTextBox2.Lines.Count() == 0;
            string evStr = str.Substring(pos + pat.Length);
            string fStr = str.Substring(0, pos);
            evStr = evStr.Substring(0, evStr.Length - 1);
           
            double prob = double.Parse(evStr, 
                NumberStyles.Any, 
                CultureInfo.GetCultureInfo("en-US")) * 100;
            richTextBox2.AppendText(fStr + ": " + prob.ToString("0.000", CultureInfo.GetCultureInfo("en-US")) + "%\n");
            if (wasEmpty)
            {
                richTextBox2.SelectAll();
                richTextBox2.SelectionFont = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
            }
        }
    }
}
