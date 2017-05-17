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
        private readonly string _connectionName;

        public DbNode(string connectionName, string serverAndDatabase) : base($"{connectionName} - {serverAndDatabase}")
        {
            _connectionName = connectionName;
            ImageKey = "Database";
            SelectedImageKey = "Database";
        }

        public string ConnectionName {  get { return _connectionName; } }
    }
}
