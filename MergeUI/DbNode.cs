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
        public DbNode(string connectionName, IDbConnection connection) : base($"{connectionName} - {ParseConnectionInfo(connection)}")
        {
            ImageKey = "Database";
            SelectedImageKey = "Database";
        }

        private static string ParseConnectionInfo(IDbConnection connection)
        {
            Dictionary<string, string> nameParts = connection
                .ConnectionString.Split(';')
                .Select(s =>
                {
                    string[] parts = s.Split('=');
                    return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
                }).ToDictionary(item => item.Key, item => item.Value);

            return $"{Coalesce(nameParts, "Data Source", "Server")}.{Coalesce(nameParts, "Database", "Initial Catalog")}";
        }

        private static string Coalesce(Dictionary<string, string> dictionary, params string[] keys)
        {
            string key = keys.First(item => dictionary.ContainsKey(item));
            return dictionary[key];
        }
    }
}
