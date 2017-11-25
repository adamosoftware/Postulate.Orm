using MySql.Data.MySqlClient;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge;
using Postulate.Orm.MySql;
using Postulate.Orm.SqlServer;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.MergeUI
{
    internal class ScriptManager
    {
        public SupportedSyntax CurrentSyntax { get; private set; }
        public Assembly Assembly { get; private set; }
        public Configuration Configuration { get; private set; }
        public string[] ConnectionNames { get; private set; }
        public SqlSyntax Syntax { get; private set; }
        public IEnumerable<MergeAction> Actions { get; private set; }
        public Dictionary<MergeAction, LineRange> LineRanges { get; private set; }
        public StringBuilder Script { get; private set; }
        public ILookup<MergeAction, string> ValidationErrors { get; private set; }        
        public Stopwatch Stopwatch { get; private set; }

        private ScriptManager()
        {
            // use FromFile
        }

        public static ScriptManager FromFile(string fileName, SupportedSyntax? syntax = null)
        {
            var assembly = Assembly.LoadFile(fileName);

            DefaultSqlSyntaxAttribute defaultSyntax;
            SupportedSyntax currentSyntax = syntax ??
                ((assembly.HasAttribute(out defaultSyntax)) ? defaultSyntax.Syntax :
                    throw new ArgumentException("Could not determine the SQL syntax. Please specify the SupportedSyntax argument or use the [DefaultSqlSyntaxAttribute] on the assembly."));

            var result = new ScriptManager();
            result.Assembly = assembly;
            result.Configuration = ConfigurationManager.OpenExeConfiguration(assembly.Location);
            result.CurrentSyntax = currentSyntax;

            List<string> connectionNames = new List<string>();
            foreach (ConnectionStringSettings connectionStr in result.Configuration.ConnectionStrings.ConnectionStrings)
            {
                if (!IsLocalConfigElement(connectionStr, result.Configuration.FilePath)) continue;

                if (ConnectionProviders[currentSyntax].ProviderNames.Contains(connectionStr.ProviderName) ||
                    OpensSuccessfully(currentSyntax, connectionStr.ConnectionString))
                {
                    connectionNames.Add(connectionStr.Name);
                }
            }
            result.ConnectionNames = connectionNames.ToArray();

            return result;
        }

        private static bool OpensSuccessfully(SupportedSyntax currentSyntax, string connectionString)
        {
            switch (currentSyntax)
            {
                case SupportedSyntax.MySql:
                    return TryConnection(() => new MySqlConnection(connectionString));

                case SupportedSyntax.SqlServer:
                    return TryConnection(() => new SqlConnection(connectionString));
            }

            return false;
        }

        private static bool TryConnection(Func<IDbConnection> connector)
        {
            try
            {
                using (var cn = connector.Invoke())
                {
                    cn.Open();
                    return true;
                }                
            }
            catch 
            {
                return false;
            }
        }

        private static bool IsLocalConfigElement(ConnectionStringSettings connectionStr, string fileName)
        {
            return connectionStr.ElementInformation.Properties["name"].Source.Equals(fileName);
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
                    await GenerateScriptInnerAsync<MySqlSyntax>(db, showProgress);
                    break;

                case SupportedSyntax.SqlServer:                    
                    await GenerateScriptInnerAsync<SqlServerSyntax>(db, showProgress);
                    break;
            }
        }

        private async Task GenerateScriptInnerAsync<TSyntax>(SqlDb<int> db, IProgress<MergeProgress> showProgress) where TSyntax : SqlSyntax, new()
        {
            Syntax = new TSyntax(); // this is so I can still get the CommentPrefix in the script output if no actions are found

            db.CreateIfNotExists();

            var engine = new Engine<TSyntax>(Assembly, showProgress);

            using (var cn = db.GetConnection())
            {
                cn.Open();
                Actions = await engine.CompareAsync(cn);
                Stopwatch = engine.Stopwatch;
                Dictionary<MergeAction, LineRange> lineRanges;
                Script = engine.GetScript(cn, Actions, out lineRanges);
                LineRanges = lineRanges;
                ValidationErrors = Actions
                    .SelectMany(item => item.ValidationErrors(cn).Select(msg => new { Action = item, Message = msg }))
                    .ToLookup(item2 => item2.Action, item2 => item2.Message);
            }
        }

        public string ScriptSelectedActions(string connectionName, IEnumerable<MergeAction> actions, out Dictionary<MergeAction, LineRange> lineRanges)
        {
            var db = ConnectionProviders[CurrentSyntax].GetDb.Invoke(this.Configuration, connectionName);
            switch (CurrentSyntax)
            {
                case SupportedSyntax.MySql:
                    return ScripSelectedActionsInner<MySqlSyntax>(db, actions, out lineRanges);

                case SupportedSyntax.SqlServer:
                    return ScripSelectedActionsInner<SqlServerSyntax>(db, actions, out lineRanges);                    
            }

            throw new ArgumentException($"Unrecognized CurrentSyntax setting {CurrentSyntax}.");
        }

        private string ScripSelectedActionsInner<TSyntax>(SqlDb<int> db, IEnumerable<MergeAction> actions, out Dictionary<MergeAction, LineRange> lineRanges) where TSyntax : SqlSyntax, new()
        {
            var engine = new Engine<TSyntax>(Assembly, null);
            using (var cn = db.GetConnection())
            {
                return engine.GetScript(cn, actions, out lineRanges).ToString();
            }                
        }

        public async Task ExecuteSelectedActionsAsync(string connectionName, IEnumerable<MergeAction> actions, IProgress<MergeProgress> showProgress)
        {
            var db = ConnectionProviders[CurrentSyntax].GetDb.Invoke(this.Configuration, connectionName);
            switch (CurrentSyntax)
            {
                case SupportedSyntax.MySql:
                    await ExecuteSelectedActionsInnerAsync<MySqlSyntax>(db, actions, showProgress);
                    return;

                case SupportedSyntax.SqlServer:
                    await ExecuteSelectedActionsInnerAsync<SqlServerSyntax>(db, actions, showProgress);
                    return;
            }

            throw new ArgumentException($"Unrecognized CurrentSyntax setting {CurrentSyntax}.");
        }

        private async Task ExecuteSelectedActionsInnerAsync<TSyntax>(SqlDb<int> db, IEnumerable<MergeAction> actions, IProgress<MergeProgress> progress) where TSyntax : SqlSyntax, new()
        {
            var engine = new Engine<TSyntax>(Assembly, progress);
            using (var cn = db.GetConnection())
            {
                await engine.ExecuteAsync(cn, actions);
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
        /// Method that returns a live SqlDb when given a configuration and connection name
        /// </summary>
        public Func<Configuration, string, SqlDb<int>> GetDb { get; private set; }
    }
}