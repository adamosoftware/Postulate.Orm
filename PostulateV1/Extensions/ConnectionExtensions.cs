using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Extensions
{
    public static class ConnectionExtensions
    {
        public static bool Exists(this IDbConnection connection, string fromWhere, object parameters = null, IDbTransaction transaction = null)
        {
            return ((connection.QueryFirstOrDefault<int?>($"SELECT 1 FROM {fromWhere}", parameters, transaction) ?? 0) == 1);
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

        public static bool ReferencedTableExists(this IDbConnection connection, PropertyInfo propertyInfo)
        {
            ForeignKeyAttribute fk = propertyInfo.GetForeignKeyAttribute();
            if (fk != null) return TableExists(connection, fk.PrimaryTableType);
            throw new ArgumentException($"{propertyInfo.Name} is not a foreign key.");
        }

        public static bool IsTableEmpty(this IDbConnection connection, string schema, string tableName)
        {
            return ((connection.QueryFirstOrDefault<int?>($"SELECT COUNT(1) FROM [{schema}].[{tableName}]", null) ?? 0) == 0);
        }

        public static bool IsColumnInPrimaryKey(this IDbConnection connection, string schema, string tableName, string columnName, out string pkName)
        {
            ColumnRef cr = new ColumnRef() { Schema = schema, TableName = tableName, ColumnName = columnName };

            var keyColumns = GetKeyColumns(connection, keyInfo => keyInfo.IsPrimaryKey && keyInfo.Equals(cr));
            if (keyColumns.Any())
            {
                pkName = keyColumns.First().IndexName;
                return true;
            }

            pkName = null;
            return false;
        }

        public static bool IsColumnInPrimaryKey(this IDbConnection connection, ColumnRef columnRef, out string pkName)
        {
            return IsColumnInPrimaryKey(connection, columnRef.Schema, columnRef.TableName, columnRef.ColumnName, out pkName);
        }

        public static IEnumerable<KeyColumnInfo> GetKeyColumns(this IDbConnection connection, Func<KeyColumnInfo, bool> filter = null)
        {
            var results = connection.Query<KeyColumnInfo>(
                @"SELECT
                    SCHEMA_NAME([t].[schema_id]) AS [Schema],
                    [t].[name] AS [TableName],
                    [col].[name] AS [ColumnName],
                    [ndx].[name] AS [IndexName],
                    [ndx].[type] AS [IndexType],
                    [ndx].[is_unique] AS [IsUnique],
                    [ndx].[is_primary_key] AS [IsPrimaryKey]
                FROM
                    [sys].[index_columns] [ndxcol] INNER JOIN [sys].[indexes] [ndx] ON
                        [ndxcol].[object_id]=[ndx].[object_id] AND
                        [ndxcol].[index_id]=[ndx].[index_id]
                    INNER JOIN [sys].[tables] [t] ON [ndx].[object_id]=[t].[object_id]
                    INNER JOIN [sys].[columns] [col] ON
                         [ndxcol].[object_id]=[col].[object_id] AND
                         [ndxcol].[column_id]=[col].[column_id]");

            return (filter == null) ?
                results :
                results.Where(row => filter.Invoke(row));
        }
    }

    public class KeyColumnInfo
    {
        public string Schema { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string IndexName { get; set; }
        public byte IndexType { get; set; }
        public bool IsUnique { get; set; }
        public bool IsPrimaryKey { get; set; }

        public override bool Equals(object obj)
        {
            ColumnRef cr = obj as ColumnRef;
            if (cr != null)
            {
                return
                    Schema.ToLower().Equals(cr.Schema.ToLower()) &&
                    TableName.ToLower().Equals(cr.TableName.ToLower()) &&
                    ColumnName.ToLower().Equals(cr.ColumnName.ToLower());
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}