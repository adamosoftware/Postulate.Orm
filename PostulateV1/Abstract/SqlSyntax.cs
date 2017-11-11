using Dapper;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Abstract
{
    public abstract class SqlSyntax
    {
        public abstract string CommentPrefix { get; }

        public abstract string CommandSeparator { get; }

        public abstract string ApplyDelimiter(string objectName);

        public abstract string GetTableName(Type type);      

        public abstract string TableExistsQuery { get; }

        public abstract object TableExistsParameters(Type type);

        public abstract string ColumnExistsQuery { get; }

        public abstract object ColumnExistsParameters(PropertyInfo propertyInfo);

        public abstract string IndexExistsQuery { get; }

        public abstract string SchemaColumnQuery { get; }

        public abstract object SchemaColumnParameters(Type type);

        public abstract bool FindObjectId(IDbConnection connection, TableInfo tableInfo);

        public abstract string SqlDataType(PropertyInfo propertyInfo);

        public abstract bool IsTableEmpty(IDbConnection connection, Type t);        

        public bool TableExists(IDbConnection connection, Type t)
        {
            return connection.Exists(TableExistsQuery, TableExistsParameters(t));
        }

        public bool ColumnExists(IDbConnection connection, PropertyInfo pi)
        {
            return connection.Exists(ColumnExistsQuery, ColumnExistsParameters(pi));
        }

        public bool IndexExists(IDbConnection connection, string name)
        {
            return connection.Exists(IndexExistsQuery, new { name = name });
        }

        public abstract string AddColumnStatement(TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false);

        public abstract string AlterColumnStatement(TableInfo tableInfo, PropertyInfo propertyInfo);

        public abstract string UpdateColumnWithExpressionStatement(TableInfo tableInfo, PropertyInfo propertyInfo, string expression);

        public abstract ILookup<int, ColumnInfo> GetSchemaColumns(IDbConnection connection);

        public abstract IEnumerable<TableInfo> GetSchemaTables(IDbConnection connection);

        protected abstract string GetExcludeSchemas(IDbConnection connection);

        public abstract IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo);

        public abstract Dictionary<Type, string> KeyTypeMap(bool withDefaults = true);

        public abstract Dictionary<Type, string> SupportedTypes(string length = null, byte precision = 0, byte scale = 0);

        public bool IsSupportedType(Type type)
        {
            return
               SupportedTypes().ContainsKey(type) ||
               (type.IsEnum && type.GetEnumUnderlyingType().Equals(typeof(int))) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSupportedType(type.GetGenericArguments()[0]));
        }

        public abstract string GetColumnSyntax(PropertyInfo propertyInfo, bool forceNull = false);        

        public abstract string GetColumnType(PropertyInfo propertyInfo, bool forceNull = false);

        public abstract string GetColumnDefault(PropertyInfo propertyInfo, bool forCreateTable = false);

        public abstract string GetConstraintBaseName(Type type);

        public abstract string GetDropForeignKeyStatement(ForeignKeyInfo foreignKeyInfo);

        public abstract string GetDropTableStatement(TableInfo tableInfo);

        public abstract string GetCreateTableStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns);

        public abstract string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns);

        public abstract IEnumerable<KeyColumnInfo> GetKeyColumns(IDbConnection connection, Func<KeyColumnInfo, bool> filter = null);

        public abstract string GetCopyStatement<TRecord, TKey>(IEnumerable<string> paramColumns, IEnumerable<string> columns) where TRecord : Record<TKey>;

        public abstract string GetForeignKeyStatement(PropertyInfo propertyInfo);

        public abstract string GetCreateColumnIndexStatement(PropertyInfo propertyInfo);

        public abstract TableInfo GetTableInfoFromType(Type type);
    }
}