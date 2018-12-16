using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuronDemonstrations
{
    class MyTreeNode
    {
        public TreeNode TreeNode { get; private set; }
        public int Lvl { get; private set; }
        public string Path { get; private set; }
        public MyTreeNode(TreeNode treeNode, string path, int lvl)
        {
            TreeNode = treeNode;
            Path = path;
            Lvl = lvl;
        }
    }
}
