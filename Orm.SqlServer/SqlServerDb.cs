using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Postulate.Orm.SqlServer
{
    public partial class SqlServerDb<TKey> : SqlDb<TKey>, IDb
    {
        private const string _changesSchema = "changes";
        private const string _deletedSchema = "deleted";

        public SqlServerDb(Configuration configuration, string connectionName, string userName = null) : base(configuration, connectionName, new SqlServerSyntax(), userName)
        {
        }

        public SqlServerDb(string connectionName, string userName = null) : base(connectionName, new SqlServerSyntax(), userName)
        {
        }

        public override IDbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public override IDbTransaction GetTransaction(IDbConnection connection)
        {
            return connection.BeginTransaction();
        }

        protected override object OnGetChangesPropertyValue(PropertyInfo propertyInfo, object record, IDbConnection connection)
        {
            object result = base.OnGetChangesPropertyValue(propertyInfo, record, connection);

            ForeignKeyAttribute fk;
            DereferenceExpression dr;
            if (result != null && propertyInfo.HasAttribute(out fk) && fk.PrimaryTableType.HasAttribute(out dr))
            {
                TableInfo obj = TableInfo.FromModelType(fk.PrimaryTableType);
                try
                {
                    result = connection.QueryFirst<string>(
                        $@"SELECT {dr.Expression} FROM [{obj.Schema}].[{obj.Name}]
	    				WHERE [{fk.PrimaryTableType.IdentityColumnName()}]=@id", new { id = result });
                }
                catch
                {
                    result = "<null>";
                }
            }

            return result;
        }

        public override int GetRecordNextVersion<TRecord>(TKey id)
        {
            using (var cn = GetConnection())
            {
                cn.Open();
                return GetRecordNextVersion<TRecord>(cn, id);
            }
        }

        public override int GetRecordNextVersion<TRecord>(IDbConnection connection, TKey id)
        {
            try
            {
                string tableName = ChangeTrackingTableName<TRecord>();
                return connection.QueryFirstOrDefault<int>($"SELECT [NextVersion] FROM [{_changesSchema}].[{tableName}_Versions] WHERE [RecordId]=@id", new { id = id });
            }
            catch
            {
                return 0;
            }
        }

        private string ChangeTrackingTableName<TRecord>()
        {
            TableInfo obj = Syntax.GetTableInfoFromType(typeof(TRecord));
            return $"{obj.Schema}_{obj.Name}";
        }

        protected override void OnCaptureChanges<TRecord>(IDbConnection connection, TKey id, IEnumerable<PropertyChange> changes)
        {            
            string tableName = ChangeTrackingTableName<TRecord>();

            CreateChangeTrackingTables(connection, tableName);

            string indexName = $"IX_{_changesSchema}_{tableName}_RecordId";
            if (!connection.Exists("[sys].[indexes] WHERE [name]=@name", new { name = indexName }))
            {
                connection.Execute($"CREATE INDEX [{indexName}] ON [{_changesSchema}].[{tableName}] ([RecordId])");
            }

            int version = 0;
            while (version == 0)
            {
                version = GetRecordNextVersion<TRecord>(connection, id);
                if (version == 0) connection.Execute($"INSERT INTO [{_changesSchema}].[{tableName}_Versions] ([RecordId]) VALUES (@id)", new { id = id });
            }
            connection.Execute($"UPDATE [{_changesSchema}].[{tableName}_Versions] SET [NextVersion]=[NextVersion]+1 WHERE [RecordId]=@id", new { id = id });

            foreach (var change in changes)
            {
                connection.Execute(
                    $@"INSERT INTO [{_changesSchema}].[{tableName}] ([RecordId], [Version], [UserName], [ColumnName], [OldValue], [NewValue])
					VALUES (@id, @version, @userName, @columnName, @oldValue, @newValue)",
                    new
                    {
                        id = id,
                        version = version,
                        columnName = change.PropertyName,
                        userName = UserName ?? "<unknown>",
                        oldValue = CleanMinDate(change.OldValue) ?? "<null>",
                        newValue = CleanMinDate(change.NewValue) ?? "<null>"
                    });
            }
        }

        private void CreateChangeTrackingTables(IDbConnection connection, string tableName)
        {
            if (!connection.Exists("[sys].[schemas] WHERE [name]=@name", new { name = _changesSchema })) connection.Execute($"CREATE SCHEMA [{_changesSchema}]");

            if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _changesSchema, name = $"{tableName}_Versions" }))
            {
                connection.Execute($@"CREATE TABLE [{_changesSchema}].[{tableName}_Versions] (
					[RecordId] {Syntax.KeyTypeMap(false)[typeof(TKey)]} NOT NULL,
					[NextVersion] int NOT NULL DEFAULT (1),
					CONSTRAINT [PK_{_changesSchema}_{tableName}_Versions] PRIMARY KEY ([RecordId])
				)");
            }

            if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _changesSchema, name = tableName }))
            {
                connection.Execute($@"CREATE TABLE [{_changesSchema}].[{tableName}] (
					[RecordId] {Syntax.KeyTypeMap(false)[typeof(TKey)]} NOT NULL,
					[Version] int NOT NULL,
					[ColumnName] nvarchar(100) NOT NULL,
                    [UserName] nvarchar(256) NOT NULL,
					[OldValue] nvarchar(max) NULL,
					[NewValue] nvarchar(max) NULL,
					[DateTime] datetime NOT NULL DEFAULT (getutcdate()),
					CONSTRAINT [PK_{tableName}] PRIMARY KEY ([RecordId], [Version], [ColumnName])
				)");
            }
        }

        protected override void OnCaptureDeletion<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction)
        {
            if (!connection.Exists("[sys].[schemas] WHERE [name]=@name", new { name = _deletedSchema }, transaction)) connection.Execute($"CREATE SCHEMA [{_deletedSchema}]", null, transaction);

            string tableName = ChangeTrackingTableName<TRecord>();

            if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _deletedSchema, name = tableName }, transaction))
            {
                connection.Execute($@"CREATE TABLE [{_deletedSchema}].[{tableName}] (
					[RecordId] {Syntax.KeyTypeMap(false)[typeof(TKey)]} NOT NULL,
                    [UserName] nvarchar(256) NOT NULL,
                    [Data] xml NOT NULL,
					[DateTime] datetime NOT NULL DEFAULT (getutcdate()),
					CONSTRAINT [PK_{_changesSchema}_{tableName}] PRIMARY KEY ([RecordId])
				)", null, transaction);
            }

            string recordXml = ToXml(record);

            connection.Execute(
                $@"DELETE [{_deletedSchema}].[{tableName}] WHERE [RecordId]=@id;
                INSERT INTO [{_deletedSchema}].[{tableName}] ([RecordId], [UserName], [Data]) VALUES (@id, @userName, @data)",
                new { id = record.Id, userName = UserName, data = recordXml }, transaction);
        }

        protected override TRecord BeginRestore<TRecord>(IDbConnection connection, TKey id)
        {
            TableInfo obj = Syntax.GetTableInfoFromType(typeof(TRecord));
            var xmlString = connection.QuerySingleOrDefault<string>($"SELECT [Data] FROM [deleted].[{obj.Schema}_{obj.Name}] WHERE [RecordId]=@id", new { id = id });
            if (string.IsNullOrEmpty(xmlString)) throw new Exception($"{obj.Schema}.{obj.Name} with record id {id} was not found to restore.");
            return FromXml<TRecord>(xmlString);
        }

        protected override void CompleteRestore<TRecord>(IDbConnection connection, TKey id, IDbTransaction transaction)
        {
            TableInfo obj = Syntax.GetTableInfoFromType(typeof(TRecord));
            connection.Execute($"DELETE [deleted].[{obj.Schema}_{obj.Name}] WHERE [RecordId]=@id", new { id = id }, transaction);
        }

        private static string ToXml<T>(T @object)
        {
            // thanks to https://stackoverflow.com/questions/4123590/serialize-an-object-to-xml

            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (var sw = new StringWriter())
            {
                using (var xw = XmlWriter.Create(sw))
                {
                    xs.Serialize(xw, @object);
                    return sw.ToString();
                }
            }
        }

        private static T FromXml<T>(string xml)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xml))
            {
                return (T)xs.Deserialize(reader);
            }
        }

        private static object CleanMinDate(object value)
        {
            // prevents DateTime.MinValue from getting passed to SQL Server as a parameter, where it fails
            if (value is DateTime && value.Equals(default(DateTime))) return null;
            return value;
        }

        public override IEnumerable<ChangeHistory<TKey>> QueryChangeHistory<TRecord>(IDbConnection connection, TKey id, int timeZoneOffset = 0)
        {
            TableInfo obj = Syntax.GetTableInfoFromType(typeof(TRecord));
            string tableName = $"{obj.Schema}_{obj.Name}";

            CreateChangeTrackingTables(connection, tableName);

            var results = connection.Query<ChangeHistoryRecord<TKey>>(
                $@"SELECT * FROM [{_changesSchema}].[{tableName}] WHERE [RecordId]=@id ORDER BY [DateTime] DESC", new { id = id });

            return results.GroupBy(item => new
            {
                RecordId = item.RecordId,
                Version = item.Version
            }).Select(ch =>
            {
                return new ChangeHistory<TKey>()
                {
                    RecordId = ch.Key.RecordId,
                    DateTime = ch.First().DateTime.AddHours(timeZoneOffset),
                    Version = ch.Key.Version,
                    UserName = ch.First().UserName,
                    Properties = ch.Select(chr => new PropertyChange()
                    {
                        PropertyName = chr.ColumnName,
                        OldValue = chr.OldValue,
                        NewValue = chr.NewValue
                    })
                };
            });
        }

        public string MergeExcludeSchemas { get; set; }
        public string MergeExcludeTables { get; set; }
    }
}