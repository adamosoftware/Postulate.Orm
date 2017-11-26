using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Abstract
{
    /// <summary>
    /// Base class for defining SQL dialect rules that apply to a specific database platform
    /// </summary>
    public abstract class SqlSyntax
    {
        public abstract string CommentPrefix { get; }

        public abstract string CommandSeparator { get; }

        /// <summary>
        /// Applies the platform-specific delimiter around object names, such as square brackets around SQL Server object names or backticks in MySQL
        /// </summary>        
        public abstract string ApplyDelimiter(string objectName);

        /// <summary>
        /// Returns the column name followed by the alias, if specified. Used by <see cref="SqlDb{TKey}.Find{TRecord}(TKey)"/> method to ensure column and property names are mapped 
        /// </summary>
        public string SelectExpression(PropertyInfo propertyInfo)
        {
            string result = ApplyDelimiter(propertyInfo.SqlColumnName());

            ColumnAttribute colAttr;
            if (propertyInfo.HasAttribute(out colAttr, a => !string.IsNullOrEmpty(a.Name)))
            {
                result += " AS " + ApplyDelimiter(propertyInfo.Name);
            }

            return result;
        }

        /// <summary>
        /// Returns the database table name for the given model type, applying the schema name, if specified with the [Table] or [Schema] attribute
        /// </summary>
        public abstract string GetTableName(Type type);      

        public abstract string TableExistsQuery { get; }

        public abstract object TableExistsParameters(Type type);

        public abstract string ColumnExistsQuery { get; }

        public abstract object ColumnExistsParameters(PropertyInfo propertyInfo);

        public abstract string IndexExistsQuery { get; }

        public abstract string SchemaColumnQuery { get; }

        public abstract object SchemaColumnParameters(Type type);

        public abstract bool IsColumnInPrimaryKey(IDbConnection connection, ColumnInfo fromColumn, out bool clustered, out string constraintName);

        public abstract bool FindObjectId(IDbConnection connection, TableInfo tableInfo);

        public abstract string SqlDataType(PropertyInfo propertyInfo);

        public abstract bool IsTableEmpty(IDbConnection connection, Type t);        

        public bool TableExists(IDbConnection connection, Type t)
        {
            return connection.Exists(TableExistsQuery, TableExistsParameters(t));
        }

        public abstract bool SchemaExists(IDbConnection connection, string schemaName);

        public abstract string DropPrimaryKeyStatement(TableInfo affectedTable, string pkName);

        public bool ColumnExists(IDbConnection connection, PropertyInfo pi)
        {
            return connection.Exists(ColumnExistsQuery, ColumnExistsParameters(pi));
        }

        public bool IndexExists(IDbConnection connection, string name)
        {
            return connection.Exists(IndexExistsQuery, new { name = name });
        }

        public abstract string AddColumnStatement(TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false);

        public abstract string AddPrimaryKeyStatement(TableInfo affectedTable);

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

        public abstract string ForeignKeyDropStatement(ForeignKeyInfo foreignKeyInfo);

        public abstract string TableDropStatement(TableInfo tableInfo);

        public string TableCreateStatement(Type type)
        {
            return TableCreateStatement(type, null, null, null);
        }

        public abstract string TableCreateStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns);

        public abstract string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns);

        public abstract IEnumerable<KeyColumnInfo> GetKeyColumns(IDbConnection connection, Func<KeyColumnInfo, bool> filter = null);

        public abstract string GetCopyStatement<TRecord, TKey>(IEnumerable<string> paramColumns, IEnumerable<string> columns) where TRecord : Record<TKey>;

        public abstract string ForeignKeyAddStatement(PropertyInfo propertyInfo);

        public abstract string ForeignKeyAddStatement(ForeignKeyInfo foreignKeyInfo);

        public abstract string CreateColumnIndexStatement(PropertyInfo propertyInfo);

        public abstract string CreateSchemaStatement(string name);

        public abstract TableInfo GetTableInfoFromType(Type type);

        public static IEnumerable<PropertyInfo> PrimaryKeyProperties(Type type, bool markedOnly = false)
        {
            var pkProperties = type.GetProperties().Where(pi => pi.HasAttribute<PrimaryKeyAttribute>());
            if (pkProperties.Any() || markedOnly) return pkProperties;
            return new PropertyInfo[] { type.GetProperty(type.IdentityColumnName()) };
        }

        public static IEnumerable<string> PrimaryKeyColumns(Type type, bool markedOnly = false)
        {
            return PrimaryKeyProperties(type, markedOnly).Select(pi => pi.SqlColumnName());
        }
    }
}