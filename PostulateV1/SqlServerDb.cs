using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge;
using Postulate.Orm.Merge.Action;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Postulate.Orm
{
    public class SqlServerDb<TKey> : SqlDb<TKey>, IDb
    {
        private const string _changesSchema = "changes";

        public SqlServerDb(Configuration configuration, string connectionName, string userName = null) : base(configuration, connectionName, userName)
        {
        }

        public SqlServerDb(string connectionName, string userName = null) : base(connectionName, userName)
        {
        }

        protected override string ApplyDelimiter(string name)
        {
            return string.Join(".", name.Split('.').Select(s => $"[{s}]"));
        }

        public override IDbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public TRecord Find<TRecord>(TKey id) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return Find<TRecord>(cn, id);
            }
        }

        public bool ExistsWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return ExistsWhere<TRecord>(cn, criteria, parameters);
            }
        }

        public TRecord FindWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return FindWhere<TRecord>(cn, criteria, parameters);
            }
        }

        public void Delete<TRecord>(TRecord record) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Delete(cn, record);
            }
        }

        public void Delete<TRecord>(TKey id) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Delete<TRecord>(cn, id);
            }
        }

        public void DeleteWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                DeleteWhere<TRecord>(cn, criteria, parameters);
            }
        }

        public void Save<TRecord>(TRecord record, out SaveAction action) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Save(cn, record, out action);
            }
        }

        public void Save<TRecord>(TRecord record) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                SaveAction action;
                Save(cn, record, out action);
            }
        }

        public void Update<TRecord>(TRecord record, params Expression<Func<TRecord, object>>[] setColumns) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Update(cn, record, setColumns);
            }
        }

        protected override object OnGetChangesPropertyValue(PropertyInfo propertyInfo, object record, IDbConnection connection)
        {
            object result = base.OnGetChangesPropertyValue(propertyInfo, record, connection);

            ForeignKeyAttribute fk;
            DereferenceExpression dr;
            if (result != null && propertyInfo.HasAttribute(out fk) && fk.PrimaryTableType.HasAttribute(out dr))
            {
                DbObject obj = DbObject.FromType(fk.PrimaryTableType);
                result = connection.QueryFirst<string>(
                    $@"SELECT {dr.Expression} FROM [{obj.Schema}].[{obj.Name}]
					WHERE [{fk.PrimaryTableType.IdentityColumnName()}]=@id", new { id = result });
            }

            return result;
        }

        protected override void OnCaptureChanges<TRecord>(IDbConnection connection, TKey id, IEnumerable<PropertyChange> changes)
        {
            if (!connection.Exists("[sys].[schemas] WHERE [name]=@name", new { name = _changesSchema })) connection.Execute($"CREATE SCHEMA [{_changesSchema}]");

            DbObject obj = DbObject.FromType(typeof(TRecord));
            string tableName = $"{obj.Schema}_{obj.Name}";

            if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _changesSchema, name = $"{tableName}_Versions" }))
            {
                connection.Execute($@"CREATE TABLE [{_changesSchema}].[{tableName}_Versions] (
					[RecordId] {CreateTable.KeyTypeMap(false)[typeof(TKey)]} NOT NULL,
					[NextVersion] int NOT NULL DEFAULT (1),
					CONSTRAINT [PK_{_changesSchema}_{tableName}_Versions] PRIMARY KEY ([RecordId])
				)");
            }

            string indexName = $"IX_{_changesSchema}_{tableName}_RecordId";
            if (!connection.Exists("[sys].[indexes] WHERE [name]=@name", new { name = indexName }))
            {
                connection.Execute($"CREATE INDEX [{indexName}] ON [{_changesSchema}].[{tableName}] ([RecordId])");
            }

            if (!connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = _changesSchema, name = tableName }))
            {
                connection.Execute($@"CREATE TABLE [{_changesSchema}].[{tableName}] (
					[RecordId] {CreateTable.KeyTypeMap(false)[typeof(TKey)]} NOT NULL,
					[Version] int NOT NULL,
					[ColumnName] nvarchar(100) NOT NULL,
                    [UserName] nvarchar(256) NOT NULL,
					[OldValue] nvarchar(max) NULL,
					[NewValue] nvarchar(max) NULL,
					[DateTime] datetime NOT NULL DEFAULT (getutcdate()),
					CONSTRAINT [PK_{_changesSchema}_{obj.Name}] PRIMARY KEY ([RecordId], [Version], [ColumnName])
				)");
            }

            int version = 0;
            while (version == 0)
            {
                version = connection.QueryFirstOrDefault<int>($"SELECT [NextVersion] FROM [{_changesSchema}].[{tableName}_Versions] WHERE [RecordId]=@id", new { id = id });
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

        private static object CleanMinDate(object value)
        {
            // prevents DateTime.MinValue from getting passed to SQL Server as a parameter, where it fails
            if (value is DateTime && value.Equals(default(DateTime))) return null;
            return value;
        }

        public IEnumerable<ChangeHistory<TKey>> QueryChangeHistory<TRecord>(TKey id, int timeZoneOffset = 0) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return QueryChangeHistory<TRecord>(cn, id, timeZoneOffset);
            }
        }

        public override IEnumerable<ChangeHistory<TKey>> QueryChangeHistory<TRecord>(IDbConnection connection, TKey id, int timeZoneOffset = 0)
        {
            DbObject obj = DbObject.FromType(typeof(TRecord));
            string tableName = $"{obj.Schema}_{obj.Name}";

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
    }
}