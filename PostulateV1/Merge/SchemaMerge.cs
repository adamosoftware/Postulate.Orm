using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge.Action;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Postulate.Orm.Merge
{
    public enum MergeActionType
    {
        Create,
        Alter,
        Rename,
        Drop,
        DropAndCreate
    }

    public enum MergeObjectType
    {
        Table,
        Column,
        Key,
        Index,
        ForeignKey,
        Metadata
    }

    internal delegate IEnumerable<MergeAction> GetSchemaDiffMethod(IDbConnection connection);

    public partial class SchemaMerge<TDb> : ISchemaMerge where TDb : IDb, new()
    {
        private readonly IEnumerable<Type> _modelTypes;

        public IEnumerable<MergeAction> AllActions { get; private set; }
        public ILookup<MergeAction, string> AllValidationErrors { get; private set; }
        public ILookup<MergeAction, string> AllCommands { get; private set; }
        public string SqlScript { get; private set; }        

        public SchemaMerge()
        {
            _modelTypes = typeof(TDb).Assembly.GetTypes()
                .Where(t =>
                    !t.Name.StartsWith("<>") &&
                    t.Namespace.Equals(typeof(TDb).Namespace) &&
                    !t.HasAttribute<NotMappedAttribute>() &&
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    t.IsDerivedFromGeneric(typeof(Record<>)));
        }

        public IDbConnection GetConnection()
        {
            var db = new TDb();
            return db.GetConnection();
        }

        /// <summary>
        /// Returns the merge actions that will synchronize model classes with a SQL Server database
        /// </summary>
        /// <param name="version">Model version number to apply in the generated SQL script. Use -1 to let this class determine the version number. 
        /// Use any other value to apply an explicit version number. This is necessary for the Schema Merge app so it doesn't have to use TDb's
        /// default constructor to get the version number. If it did, the Schema Merge app would fail because the default constructor assumes
        /// that a valid connection string is available in the current configuration scope. This isn't true in the context of the Schema Merge app.</param>
        public IEnumerable<MergeAction> Compare(int version = -1)
        {
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                return Compare(cn, version);
            }
        }

        public IEnumerable<MergeAction> Compare(IDbConnection connection, int version = -1)
        {
            List<MergeAction> results = new List<MergeAction>();

            var diffMethods = new GetSchemaDiffMethod[]
            {
                // create/drop
                SyncTablesAndColumns, /*, CreatePrimaryKeys, CreateUniqueKeys, CreateIndexes, */

                // alter
                AlterColumnTypes, AlterPrimaryKeys, AlterForeignKeys, RenameTables, RenameColumns
                /* AlterUniqueKeys, AlterIndexes, AlterNonKeyColumnTypes, */
            };
            foreach (var method in diffMethods) results.AddRange(method.Invoke(connection));
            
            if (version == -1)
            {
                TDb db = new TDb();
                version = db.Version;
            }

            if (version > 0) results.Add(new SetPatchVersion(version));

            AllActions = results;

            AllValidationErrors = results.SelectMany(a => a.ValidationErrors(connection)
                .Select(err => new { Action = a, Message = err }))
                .ToLookup(item => item.Action, item => item.Message);

            AllCommands = results.SelectMany(a => a.SqlCommands(connection)
                .Where(s => !s.StartsWith("--")) // exclude comments
                .Select(cmd => new { Action = a, Command = cmd }))
                .ToLookup(item => item.Action, item => item.Command);

            SqlScript = GetScript(connection, results).ToString();

            return results;
        }

        public void Validate(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            if (actions.Any(a => !a.IsValid(connection)))
            {
                string message = string.Join("\r\n", ValidationErrors(connection, actions));
                throw new ValidationException($"The model has one or more validation errors:\r\n{message}");
            }
        }

        public void SaveScriptAs(string fileName)
        {
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                SaveScriptAs(cn, fileName);
            }
        }

        public StringBuilder GetScript(IDbConnection connection, IEnumerable<MergeAction> actions, out Dictionary<MergeAction, LineRange> lineRanges)
        {
            lineRanges = new Dictionary<MergeAction, LineRange>();
            int startRange = 0;
            int endRange = 0;

            StringBuilder sb = new StringBuilder();

            foreach (var action in actions)
            {
                foreach (var cmd in action.SqlCommands(connection))
                {
                    sb.AppendLine(cmd);
                    sb.AppendLine("\r\nGO\r\n");
                    endRange += GetLineCount(cmd) + 4;
                }

                lineRanges.Add(action, new LineRange(startRange, endRange));
                startRange = endRange;
            }

            return sb;
        }

        private int GetLineCount(string text)
        {
            int result = 0;
            int start = 0;
            while (true)
            {
                int position = text.IndexOf("\r\n", start);
                if (position == -1) break;
                start = position + 1;
                result++;
            }
            return result;
        }

        public StringBuilder GetScript(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            Dictionary<MergeAction, LineRange> lineRanges;
            return GetScript(connection, actions, out lineRanges);
        }

        public void SaveScriptAs(IDbConnection connection, string fileName)
        {
            var diffs = Compare(connection);
            using (var file = File.CreateText(fileName))
            {
                var sb = GetScript(connection, diffs);
                file.Write(sb.ToString());
            }
        }

        public bool IsPatched(Func<string, int, bool> uiAction = null)
        {
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                return IsPatched(cn, uiAction);
            }
        }

        public bool IsPatched(IDbConnection connection, Func<string, int, bool> uiAction = null)
        {
            int schemaVersion;

            if (IsPatchAvailable(connection, out schemaVersion))
            {                
                var actions = Compare(connection);
                string script = GetScript(connection, actions).ToString();

                if (uiAction != null)
                {
                    // giving user opportunity to cancel
                    if (!uiAction.Invoke(script, schemaVersion)) return false;
                }

                Execute(connection, actions);
                return true;
            }

            return true;
        }

        public void Execute()
        {
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                var actions = Compare();
                Execute(cn, actions);
            }
        }

        public void Execute(IDbConnection connection)
        {
            var actions = Compare(connection);
            Execute(connection, actions);
        }

        public void Execute(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            Validate(connection, actions);

            foreach (var diff in actions)
            {
                foreach (var cmd in diff.SqlCommands(connection))
                {
                    // add to command queue somewhere
                    // enable setting command timeout?
                    connection.Execute(cmd);
                    // set some kind of success indicator somewhere
                }
            }
        }

        public static IEnumerable<ValidationError> ValidationErrors(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            return actions.Where(a => !a.IsValid(connection)).SelectMany(a => a.ValidationErrors(connection), (a, m) => new ValidationError(a, m));
        }

        public static bool IsPatchAvailable(IDbConnection connection, out int modelVersion)
        {
            int schemaVersion = GetSchemaVersion(connection);
            modelVersion = (new TDb()).Version;
            return (modelVersion > schemaVersion);
        }

        private static int GetSchemaVersion(IDbConnection connection)
        {
            var ver = new SetPatchVersion(0);
            foreach (var cmd in ver.SqlCommands(connection)) connection.Execute(cmd);
            return connection.QuerySingle<int?>($"SELECT MAX([Version]) FROM [{SetPatchVersion.MetaSchema}].[{SetPatchVersion.TableName}]", null) ?? 0;
        }

        public static bool IsSupportedType(Type type)
        {
            return
                CreateTable.SupportedTypes().ContainsKey(type) ||
                (type.IsEnum && type.GetEnumUnderlyingType().Equals(typeof(int))) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSupportedType(type.GetGenericArguments()[0]));
        }

        private IEnumerable<ColumnRef> GetSchemaColumns(IDbConnection connection)
        {
            return connection.Query<ColumnRef>(
                @"SELECT SCHEMA_NAME([t].[schema_id]) AS [Schema], [t].[name] AS [TableName], [c].[Name] AS [ColumnName],
					[t].[object_id] AS [ObjectID], TYPE_NAME([c].[system_type_id]) AS [DataType],
					[c].[max_length] AS [ByteLength], [c].[is_nullable] AS [IsNullable],
					[c].[precision] AS [Precision], [c].[scale] as [Scale], [c].[collation_name] AS [Collation]
				FROM
					[sys].[tables] [t] INNER JOIN [sys].[columns] [c] ON [t].[object_id]=[c].[object_id]
                WHERE
                    SCHEMA_NAME([t].[schema_id]) NOT IN ('changes', 'meta', 'deleted') AND
                    [t].[name] NOT LIKE 'AspNet%' AND
                    [t].[name] NOT LIKE '__MigrationHistory'");
        }

        private IEnumerable<ColumnRef> GetModelColumns(IDbConnection collationLookupConnection = null)
        {
            var results = _modelTypes.SelectMany(t => t.GetProperties()
                .Where(pi => !pi.HasAttribute<NotMappedAttribute>())
                .Select(pi => new ColumnRef(pi) { ModelType = t }));

            if (collationLookupConnection != null)
            {
                var collations = collationLookupConnection.Query<ColumnRef>(
                    @"SELECT
	                    SCHEMA_NAME([tbl].[schema_id]) AS [Schema],
	                    [tbl].[name] AS [TableName],
	                    [col].[Name] AS [ColumnName],
	                    [col].[collation_name] AS [Collation]
                    FROM
	                    [sys].[columns] [col] INNER JOIN [sys].[tables] [tbl] ON [col].[object_id]=[tbl].[object_id]
                    WHERE
	                    [col].[collation_name] IS NOT NULL");

                results = from cr in results
                          join col in collations on
                            new { Schema = cr.Schema, TableName = cr.TableName, ColumnName = cr.ColumnName } equals
                            new { Schema = col.Schema, TableName = col.TableName, ColumnName = col.ColumnName } into collatedColumns
                          from output in collatedColumns.DefaultIfEmpty()
                          select new ColumnRef(cr.PropertyInfo) { Collation = output?.Collation };
            }

            return results;
        }

        private static IEnumerable<DbObject> GetSchemaTables(IDbConnection connection)
        {
            var tables = connection.Query(
                @"SELECT
                    SCHEMA_NAME([t].[schema_id]) AS [Schema], [t].[name] AS [Name], [t].[object_id] AS [ObjectId]
                FROM
                    [sys].[tables] [t]
                WHERE
                    SCHEMA_NAME([t].[schema_id]) NOT IN ('changes', 'meta', 'deleted') AND
                    [name] NOT LIKE 'AspNet%' AND
                    [name] NOT LIKE '__MigrationHistory'");
            return tables.Select(item => new DbObject(item.Schema, item.Name, item.ObjectId));
        }

        internal Type FindModelType(string schema, string name)
        {
            return FindModelType(new DbObject(schema, name));
        }

        internal Type FindModelType(DbObject @object)
        {
            return _modelTypes.FirstOrDefault(t =>
            {
                DbObject obj = DbObject.FromType(t);
                return obj.Schema.Equals(@object.Schema) && obj.Name.Equals(@object.Name);
            });
        }

        public class ValidationError
        {
            private readonly MergeAction _diff;
            private readonly string _message;

            public ValidationError(MergeAction diff, string message)
            {
                _diff = diff;
                _message = message;
            }

            public MergeAction Diff => _diff;

            public override string ToString()
            {
                return $"{_diff.ToString()}: {_message}";
            }
        }
    }
}