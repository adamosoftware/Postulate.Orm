using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.SqlCe
{
    public class SqlCeSyntax : SqlSyntax
    {
		public override IDbConnection GetConnection(string connectionString)
		{
			return new SqlCeConnection(connectionString);
		}

		public override bool SupportsSchemas => false;

		/// <summary>
		/// See https://technet.microsoft.com/en-us/library/ms174147(v=sql.110).aspx
		/// </summary>
		public override string CommentPrefix => "--";

        /// <summary>
        /// I don't think SqlCe supports batching, but I have to put something here
        /// </summary>
        public override string CommandSeparator => ";\r\n";

        public override string TableExistsQuery => throw new NotImplementedException();

        public override string ColumnExistsQuery => throw new NotImplementedException();

        public override string IndexExistsQuery => throw new NotImplementedException();

        public override string SchemaColumnQuery => throw new NotImplementedException();

        public override string ApplyDelimiter(string objectName)
        {
            return $"[{objectName}]";
        }

        public override string ApplyPaging(string sql, int pageNumber, int rowsPerPage)
        {
            throw new NotImplementedException();
        }

        public override string CheckEnumValueExistsStatement(string tableName)
        {
            throw new NotImplementedException();
        }

        public override string ColumnAddStatement(TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false)
        {
            throw new NotImplementedException();
        }

        public override string ColumnAlterStatement(TableInfo tableInfo, PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string ColumnDropStatement(ColumnInfo columnInfo)
        {
            throw new NotImplementedException();
        }

        public override object ColumnExistsParameters(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string CreateColumnIndexStatement(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string CreateEnumTableStatement(Type enumType)
        {
            throw new NotImplementedException();
        }

        public override string CreateSchemaStatement(string name)
        {
            throw new NotImplementedException();
        }

        public override string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns, bool withForeignKeys = false)
        {
            List<string> results = new List<string>();

            results.AddRange(CreateTableColumns(type));

            results.Add(CreateTablePrimaryKey(type));

            results.AddRange(CreateTableUniqueConstraints(type));

            if (withForeignKeys)
            {
                var foreignKeys = type.GetForeignKeys();
                results.AddRange(foreignKeys.Select(pi => ForeignKeyConstraintSyntax(pi).RemoveAll("\r\n", "\t")));
            }

            return results.ToArray();
        }

        private IEnumerable<string> CreateTableUniqueConstraints(Type type)
        {
            List<string> results = new List<string>();

            if (PrimaryKeyColumns(type, markedOnly: true).Any())
            {
                results.Add($"CONSTRAINT [U_{GetConstraintBaseName(type)}_Id] UNIQUE ([Id])");
            }

            results.AddRange(type.GetProperties().Where(pi => pi.HasAttribute<UniqueKeyAttribute>()).Select(pi =>
            {
                UniqueKeyAttribute attr = pi.GetCustomAttribute<UniqueKeyAttribute>();
                return $"CONSTRAINT [U_{GetConstraintBaseName(type)}_{pi.SqlColumnName()}] UNIQUE ([{pi.SqlColumnName()}])";
            }));

            results.AddRange(type.GetCustomAttributes<UniqueKeyAttribute>().Select((u, i) =>
            {
                string constrainName = (string.IsNullOrEmpty(u.ConstraintName)) ? $"U_{GetConstraintBaseName(type)}_{i}" : u.ConstraintName;
                return $"CONSTRAINT [{constrainName}] UNIQUE {u.GetClusteredSyntax()}({string.Join(", ", u.ColumnNames.Select(col => $"[{col}]"))})";
            }));

            return results;

        }

        private string CreateTablePrimaryKey(Type type)
        {
            return $"CONSTRAINT [PK_{GetConstraintBaseName(type)}] PRIMARY KEY ({string.Join(", ", PrimaryKeyColumns(type).Select(col => $"{ApplyDelimiter(col)}"))})";
        }

        private IEnumerable<string> CreateTableColumns(Type type)
        {
            List<string> results = new List<string>();

            Position identityPos = Position.StartOfTable;
            var ip = type.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip == null) ip = type.BaseType.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip != null) identityPos = ip.Position;

            if (identityPos == Position.StartOfTable) results.Add(IdentityColumnSql(type));

            results.AddRange(ColumnProperties(type).Select(pi =>
            {
                return GetColumnSyntax(pi);
            }));

            if (identityPos == Position.EndOfTable) results.Add(IdentityColumnSql(type));

            return results;
        }

        public override bool FindObjectId(IDbConnection connection, TableInfo tableInfo)
        {
            throw new NotImplementedException();
        }

        public override string ForeignKeyAddStatement(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string ForeignKeyAddStatement(ForeignKeyInfo foreignKeyInfo)
        {
            throw new NotImplementedException();
        }

        public override string ForeignKeyDropStatement(ForeignKeyInfo foreignKeyInfo)
        {
            throw new NotImplementedException();
        }

        public override string GetColumnDefault(PropertyInfo propertyInfo, bool forCreateTable = false)
        {
            throw new NotImplementedException();
        }

        public override string GetColumnSyntax(PropertyInfo propertyInfo, bool forceNull = false)
        {
            return $"{ApplyDelimiter(propertyInfo.SqlColumnName())} {GetColumnType(propertyInfo)}";
        }

        public override string GetColumnType(PropertyInfo propertyInfo, bool forceNull = false)
        {
            string nullable = ((propertyInfo.AllowSqlNull() || forceNull) ? "NULL" : "NOT NULL");

            string result = SqlDataType(propertyInfo);

            return $"{result} {nullable}";
        }

        public override string GetConstraintBaseName(Type type)
        {
            return GetTableInfoFromType(type).Name;
        }

        public override string GetCopyStatement<TRecord, TKey>(IEnumerable<string> paramColumns, IEnumerable<string> columns)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<KeyColumnInfo> GetKeyColumns(IDbConnection connection, Func<KeyColumnInfo, bool> filter = null)
        {
            throw new NotImplementedException();
        }

        public override ILookup<int, ColumnInfo> GetSchemaColumns(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TableInfo> GetSchemaTables(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public override string GetScriptFromSaveException(SaveException exception)
        {
            throw new NotImplementedException();
        }

        public override TableInfo GetTableInfoFromType(Type type)
        {
            if (type.HasAttribute<SchemaAttribute>()) throw new NotSupportedException("[Schema] attribute is not supported in SQL Server Compact Edition");
            return TableInfo.FromModelType(type);
        }

        public override string GetTableName(Type type)
        {
            var tableInfo = GetTableInfoFromType(type);
            return ApplyDelimiter(tableInfo.Name);
        }

        public override string InsertEnumValueStatement(string tableName, string name, int value)
        {
            throw new NotImplementedException();
        }

        public override bool IsColumnInPrimaryKey(IDbConnection connection, ColumnInfo columnInfo, out bool clustered, out string constraintName)
        {
            throw new NotImplementedException();
        }

        public override bool IsTableEmpty(IDbConnection connection, Type t)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<Type, string> KeyTypeMap(bool withDefaults = true)
        {
            return new Dictionary<Type, string>()
            {
                { typeof(int), $"int{((withDefaults) ? " identity(1,1)" : string.Empty)}" },
            };
        }

        public override string PrimaryKeyAddStatement(TableInfo affectedTable)
        {
            throw new NotImplementedException();
        }

        public override string PrimaryKeyDropStatement(TableInfo affectedTable, string pkName)
        {
            throw new NotImplementedException();
        }

        public override object SchemaColumnParameters(Type type)
        {
            throw new NotImplementedException();
        }

        public override bool SchemaExists(IDbConnection connection, string schemaName)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<Type, string> SupportedTypes(string length = null, byte precision = 0, byte scale = 0)
        {
            return new Dictionary<Type, string>()
            {
                { typeof(bool), "bit" },
                { typeof(Byte), "tinyint" },
                { typeof(Byte[]), $"varbinary({length})" },
                { typeof(char), $"char({length})" },
                { typeof(DateTime), "datetime" },
                { typeof(decimal), "numeric" },
                { typeof(double), "float" },
                { typeof(short), "smallint" },
                { typeof(int), "int" },
                { typeof(long), "bigint" },
                { typeof(Single), "real" },
                { typeof(string), (length?.Equals("max") ?? false) ? "ntext" : $"nvarchar({length})" }
            };
        }

		public override string TableCreateStatement(IDbConnection connection, TableInfo tableInfo)
		{
			throw new NotImplementedException();
		}

		public override string TableCreateStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns, bool withForeignKeys = false)
        {
            return $"CREATE TABLE {GetTableName(type)} (\r\n\t" +
                string.Join(",\r\n\t", CreateTableMembers(type, addedColumns, modifiedColumns, deletedColumns, withForeignKeys)) +
            "\r\n)";
        }

        public override string TableDropStatement(TableInfo tableInfo)
        {
            throw new NotImplementedException();
        }

        public override object TableExistsParameters(Type type)
        {
            throw new NotImplementedException();
        }

        public override string UpdateColumnWithExpressionStatement(TableInfo tableInfo, PropertyInfo propertyInfo, string expression)
        {
            throw new NotImplementedException();
        }

        protected override string GetExcludeSchemas(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

		public override IEnumerable<TableInfo> GetTables(IDbConnection connection)
		{
			throw new NotImplementedException();
		}
	}
}