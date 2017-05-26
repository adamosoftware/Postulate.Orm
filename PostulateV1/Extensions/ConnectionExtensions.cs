using System.Data;
using Dapper;
using System.Reflection;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Postulate.Orm.Extensions
{
	public static class ConnectionExtensions
	{
		public static bool Exists(this IDbConnection connection, string fromWhere, object parameters = null)
		{
			return ((connection.QueryFirstOrDefault<int?>($"SELECT 1 FROM {fromWhere}", parameters) ?? 0) == 1);
		}

		public static bool ForeignKeyExists(this IDbConnection connection, string name)
		{
			return connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = name });
		}

        public static bool ForeignKeyExists(this IDbConnection connection, PropertyInfo propertyInfo)
        {
            return ForeignKeyExists(connection, propertyInfo.ForeignKeyName());
        }

		public static bool ColumnExists(this IDbConnection connection, string schema, string tableName, string columnName)
		{
			return connection.Exists(
				@"[sys].[columns] [col] INNER JOIN [sys].[tables] [tbl] ON [col].[object_id]=[tbl].[object_id]
				WHERE SCHEMA_NAME([tbl].[schema_id])=@schema AND [tbl].[name]=@tableName AND [col].[name]=@columnName",
				new { schema = schema, tableName = tableName, columnName = columnName });
		}

        public static bool TableExists(this IDbConnection connection, string schema, string tableName)
        {
            return connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = schema, name = tableName });
        }

        public static bool TableExists(this IDbConnection connection, Type modelType)
        {
            DbObject obj = DbObject.FromType(modelType);
            return TableExists(connection, obj.Schema, obj.Name);
        }

        public static bool IsTableEmpty(this IDbConnection connection, string schema, string tableName)
        {
            return ((connection.QueryFirstOrDefault<int?>($"SELECT COUNT(1) FROM [{schema}].[{tableName}]", null) ?? 0) == 0);
        }
    }
}
