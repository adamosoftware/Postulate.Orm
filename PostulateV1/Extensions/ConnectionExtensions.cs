using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Models;
using System;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Extensions
{
    public static class ConnectionExtensions
    {
        public static bool Exists(this IDbConnection connection, string fromWhere, object parameters = null, IDbTransaction transaction = null)
        {
            return ((connection.QueryFirstOrDefault<int?>($"SELECT 1 FROM {fromWhere}", parameters, transaction) ?? 0) == 1);
        }

        public static bool SchemaExists(this IDbConnection connection, string name)
        {
            return connection.Exists("[sys].[schemas] WHERE [name]=@name", new { name = name });
        }

        public static bool ForeignKeyExists(this IDbConnection connection, string name)
        {
            return connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = name });
        }

        public static bool ForeignKeyExists(this IDbConnection connection, PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
            //return ForeignKeyExists(connection, propertyInfo.ForeignKeyName());
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
            TableInfo obj = TableInfo.FromModelType(modelType);
            return TableExists(connection, obj.Schema, obj.Name);
        }

        public static bool ReferencedTableExists(this IDbConnection connection, PropertyInfo propertyInfo)
        {
            ForeignKeyAttribute fk = propertyInfo.GetForeignKeyAttribute();
            if (fk != null) return TableExists(connection, fk.PrimaryTableType);
            throw new ArgumentException($"{propertyInfo.Name} is not a foreign key.");
        }

        public static bool ReferencingTableExists(this IDbConnection connection, PropertyInfo propertyInfo)
        {
            ForeignKeyAttribute fk = propertyInfo.GetForeignKeyAttribute();
            if (fk != null) return TableExists(connection, propertyInfo.DeclaringType);
            throw new ArgumentException($"{propertyInfo.Name} is not a foreign key.");
        }

        public static bool IsTableEmpty(this IDbConnection connection, string schema, string tableName)
        {
            return ((connection.QueryFirstOrDefault<int?>($"SELECT COUNT(1) FROM [{schema}].[{tableName}]", null) ?? 0) == 0);
        }
    }
}