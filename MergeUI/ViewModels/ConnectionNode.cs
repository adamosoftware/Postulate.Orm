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
        public ConnectionNode(string name) : base(name)
        {
            ConnectionName = name;
            ImageKey = "Database";
            SelectedImageKey = "Database";
        }        

        public string ConnectionName { get; private set; }
    }
}
