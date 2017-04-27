using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using Postulate.Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.IO;
using Dapper;

namespace Postulate.Merge
{
    public enum MergeActionType
    {
        Create,
        Alter,
        Rename,
        Drop
    }

    public enum MergeObjectType
    {
        Table,
        NonKeyColumn,
        Key,
        Index,
        ForeignKey
    }

    internal delegate IEnumerable<Diff> GetSchemaDiffMethod(IDbConnection connection);

    public partial class SchemaMerge<TDb, TKey> where TDb : SqlDb<TKey>, new()
    {
        private readonly IEnumerable<Type> _modelTypes;

        private const string _metaSchema = "meta";
        private const string _metaVersion = "Version";

        public SchemaMerge()
        {
            _modelTypes = typeof(TDb).Assembly.GetTypes()
                .Where(t =>
                    !t.Name.StartsWith("<>") &&         
                    t.Namespace.Equals(typeof(TDb).Namespace) &&
                    !t.HasAttribute<NotMappedAttribute>() &&
                    !t.IsAbstract &&
                    t.IsDerivedFromGeneric(typeof(Record<>)));
        }

        public IEnumerable<Diff> Compare(IDbConnection connection)
        {
            List<Diff> results = new List<Diff>();

            var diffMethods = new GetSchemaDiffMethod[]
            {
                // create
                CreateTablesAndColumns, CreatePrimaryKeys, CreateUniqueKeys, CreateIndexes, CreateForeignKeys,

                // alter
                AlterPrimaryKeys, AlterUniqueKeys, AlterIndexes, AlterNonKeyColumnTypes, AlterForeignKeys,

                // drop
                DropTables, DropNonKeyColumns, DropPrimaryKeys, DropUniqueKeys, DropIndexes
            };
            foreach (var method in diffMethods) results.AddRange(method.Invoke(connection));

            results.Add(ScriptVersionInfo(results));

            return results;
        }

        public IEnumerable<Diff> Compare()
        {            
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                return Compare(cn);
            }            
        }

        public void SaveScriptAs(string fileName)
        {
            var diffs = Compare();

            using (var file = File.CreateText(fileName))
            {
                foreach (var diff in diffs)
                {
                    foreach (var cmd in diff.SqlCommands())
                    {
                        file.WriteLine(cmd);
                        file.WriteLine("\r\nGO\r\n");
                    }
                }
            }
        }

        public static bool Patch(Func<IEnumerable<Diff>, int, bool> uiAction = null)
        {
            int schemaVersion;
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                if (IsPatchAvailable(cn, out schemaVersion))
                {
                    var sm = new SchemaMerge<TDb, TKey>();
                    var diffs = sm.Compare(cn);

                    if (uiAction != null)
                    {
                        // giving user opportunity to cancel           
                        if (!uiAction.Invoke(diffs, schemaVersion)) return false;
                    }

                    sm.Execute(cn, diffs);
                    return true;
                }
                return false;
            }
        }

        public void Execute(IDbConnection connection, IEnumerable<Diff> diffs)
        {
            foreach (var diff in diffs)
            {
                foreach (var cmd in diff.SqlCommands())
                {
                    // add to command queue somewhere
                    // enable setting command timeout?
                    connection.Execute(cmd);
                    // set some kind of success indicator somewhere
                }
            }
        }

        public void Execute()
        {
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                var diffs = Compare(cn);
                Execute(cn, diffs);
            }
        }

        public static int GetSchemaVersion()
        {
            return (new TDb()).Version;
        }

        public static bool IsPatchAvailable(IDbConnection connection, out int schemaVersion)
        {
            int currentVersion = GetDbVersion(connection);
            schemaVersion = GetSchemaVersion();
            return (schemaVersion > currentVersion);
        }

        private static int GetDbVersion(IDbConnection connection)
        {
            CreateVersionTableIfNotExists(connection);
            return connection.QuerySingle<int?>("SELECT MAX([Value]) FROM [meta].[Version]", null) ?? 0;
        }

        private static void CreateVersionTableIfNotExists(IDbConnection connection)
        {
            if (!connection.Exists("[sys].[schemas] WHERE [name]=@name", new { name = _metaSchema })) connection.Execute($"CREATE SCHEMA [{_metaSchema}]");

            if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _metaSchema, name = _metaVersion }))
            {
                connection.Execute($@"CREATE TABLE [{_metaSchema}].[{_metaVersion}] (
					[Value] int NOT NULL,					
					CONSTRAINT [PK_{_metaSchema}_{_metaVersion}] PRIMARY KEY ([Value])
				)");
            }
        }

    }
}
