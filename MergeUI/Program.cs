using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Postulate.Orm.Merge.Action;
using System.Reflection;
using System.Configuration;
using Postulate.Orm.Extensions;
using Postulate.Orm;
using Postulate.Orm.Merge;
using Postulate.Orm.Interfaces;
using System.Data;

namespace Postulate.MergeUI
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Dictionary<string, MergeInfo> actions = null;
            string fileName = null;

            if (args?.Length == 1)
            {
                string assembly = args[0];
                if (File.Exists(assembly))
                {
                    fileName = assembly;
                    actions = GetMergeActions(assembly);
                    if (!actions.Any())
                    {
                        Console.WriteLine($"Postulate: No model changes to merge for {assembly}.");
                        return;
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain() { MergeActions = actions, AssemblyFilename = fileName });
        }

        internal static Dictionary<string, MergeInfo> GetMergeActions(string assemblyFile)
        {
            Dictionary<string, MergeInfo> results = new Dictionary<string, MergeInfo>();

            var assembly = Assembly.LoadFile(assemblyFile);
            var config = ConfigurationManager.OpenExeConfiguration(assembly.Location);
            var dbTypes = assembly.GetTypes().Where(t => t.IsDerivedFromGeneric(typeof(SqlServerDb<>)));

            foreach (var dbType in dbTypes)
            {
                Type schemaMergeBaseType = typeof(SchemaMerge<>);
                var schemaMergeGenericType = schemaMergeBaseType.MakeGenericType(dbType);
                var db = Activator.CreateInstance(dbType, config) as IDb;
                using (var cn = db.GetConnection())
                {
                    cn.Open();
                    var schemaMerge = Activator.CreateInstance(schemaMergeGenericType) as ISchemaMerge;
                    var actions = schemaMerge.Compare(cn);
                    Dictionary<Orm.Merge.Action.MergeAction, LineRange> lineRanges;
                    var script = schemaMerge.GetScript(cn, actions, out lineRanges);
                    results.Add(dbType.Name, new MergeInfo { Db = db, Merge = schemaMerge, Actions = actions, LineRanges = lineRanges, Script = script, ServerAndDatabase = ParseConnectionInfo(cn) });
                }
            }

            return results;
        }

        internal static string ParseConnectionInfo(IDbConnection connection)
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
