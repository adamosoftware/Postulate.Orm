using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge;
using Postulate.Orm.MySql;
using Postulate.Orm.SqlServer;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.MergeUI.ViewModels
{
    internal class AssemblyConnector
    {
        private static string cn;

        public SupportedSyntax CurrentSyntax { get; private set; }
        public Assembly Assembly { get; private set; }
        public Configuration Configuration { get; private set; }
        public string[] ConnectionNames { get; set; }

        public IEnumerable<MergeAction> Actions { get; private set; }
        public Dictionary<MergeAction, LineRange> LineRanges { get; private set; }
        public StringBuilder Script { get; private set; }

        private AssemblyConnector()
        {
            // use FromFile
        }

        public static AssemblyConnector FromFile(string fileName, SupportedSyntax? syntax = null)
        {
            var assembly = Assembly.LoadFile(fileName);

            DefaultSqlSyntaxAttribute defaultSyntax;
            SupportedSyntax currentSyntax = syntax ??
                ((assembly.HasAttribute(out defaultSyntax)) ? defaultSyntax.Syntax :
                    throw new ArgumentException("Could not determine the SQL syntax. Please specify the SupportedSyntax argument or use the [DefaultSqlSyntaxAttribute] on the assembly."));

            var result = new AssemblyConnector();
            result.Assembly = assembly;
            result.Configuration = ConfigurationManager.OpenExeConfiguration(assembly.Location);

            List<string> connectionNames = new List<string>();
            foreach (ConnectionStringSettings connectionStr in result.Configuration.ConnectionStrings.ConnectionStrings)
            {
                if (ConnectionProviders[currentSyntax].ProviderNames.Contains(connectionStr.ProviderName))
                {
                    connectionNames.Add(connectionStr.Name);
                }
            }
            result.ConnectionNames = connectionNames.ToArray();

            return result;
        }

        public static Dictionary<SupportedSyntax, DbConnector> ConnectionProviders
        {
            get
            {
                return new Dictionary<SupportedSyntax, DbConnector>()
                {
                    { SupportedSyntax.MySql, new DbConnector(new string[] { "MySql.Data", "MySql" }, (config, connectionName) => new MySqlDb<int>(config, connectionName)) },
                    { SupportedSyntax.SqlServer, new DbConnector(new string[] { "System.Data.SqlClient", "SqlServer" }, (config, connectionName) => new SqlServerDb<int>(config, connectionName)) }
                };
            }
        }

        public async Task GenerateScriptAsync(string connectionName, IProgress<MergeProgress> showProgress)
        {
            var db = ConnectionProviders[CurrentSyntax].GetDb.Invoke(this.Configuration, connectionName);
            switch (CurrentSyntax)
            {
                case SupportedSyntax.MySql:
                    await GenerateScriptInnerAsync(db, new MySqlSyntax(), showProgress);
                    break;

                case SupportedSyntax.SqlServer:
                    await GenerateScriptInnerAsync(db, new SqlServerSyntax(), showProgress);
                    break;
            }
        }

        private async Task GenerateScriptInnerAsync<TSyntax>(SqlDb<int> db, TSyntax syntax, IProgress<MergeProgress> showProgress) where TSyntax : SqlSyntax, new()
        {
            if (db.AllowCreateIfNotExists) db.CreateIfNotExists();

            var engine = new Engine<TSyntax>(Assembly, showProgress);

            using (var cn = db.GetConnection())
            {
                cn.Open();
                var actions = await engine.CompareAsync(cn);
                Script = engine.GetScript(cn, actions);
            }
        }
    }

    internal class DbConnector
    {
        public DbConnector(string[] names, Func<Configuration, string, SqlDb<int>> getDbMethod)
        {
            ProviderNames = names;
            GetDb = getDbMethod;
        }

        /// <summary>
        /// Provider Names you can use in your config file that Merge UI will associate with a particular SQL syntax.
        /// I wanted to support both "standard" provider names but also more natural intuitive names, so that's why this is an array
        /// </summary>
        public string[] ProviderNames { get; private set; }

        /// <summary>
        /// Method that returns a live connection when given a connection string
        /// </summary>
        public Func<Configuration, string, SqlDb<int>> GetDb { get; private set; }
    }
}