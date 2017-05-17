using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    internal class DbNode : TreeNode
    {
        public DbNode(string connectionName, string serverAndDatabase) : base($"{connectionName} - {serverAndDatabase}")
        {
            ImageKey = "Database";
            SelectedImageKey = "Database";
        }
    }
}
