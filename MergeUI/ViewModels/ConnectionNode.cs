using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI.ViewModels
{
    class ConnectionNode : TreeNode
    {
        private const string dummyText = "dummy";

        public ConnectionNode(string name) : base(name)
        {
            ConnectionName = name;
            ImageKey = "Database";
            SelectedImageKey = "Database";

            var dummyNode = new TreeNode(dummyText);
            this.Nodes.Add(dummyNode);
        }        

        public string ConnectionName { get; private set; }

        internal void ClearDummyNode()
        {
            if (this.Nodes.Count == 1)
            {
                TreeNode child = this.Nodes[0];
                if (child.Text.Equals(dummyText)) this.Nodes.Remove(child);
            }
        }
    }
}
