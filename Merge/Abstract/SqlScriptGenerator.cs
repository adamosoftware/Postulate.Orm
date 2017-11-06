using Dapper;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge.Abstract
{
    public enum ActionType
    {
        Create,
        Alter,
        Rename,
        Drop,
        DropAndCreate
    }

    public enum ObjectType
    {
        Table,
        Column,
        Key,
        Index,
        ForeignKey,
        Metadata
    }

    public abstract partial class SqlScriptGenerator
    {
        public abstract string CommentPrefix { get; }

        public abstract string GetTableName(Type type);
        
        public abstract string ApplyDelimiter(string objectName);

        public abstract string IsTableEmptyQuery { get; }

        public abstract string TableExistsQuery { get; }

        public abstract object TableExistsParameters(Type type);

        public abstract string ColumnExistsQuery { get; }

        public abstract object ColumnExistsParameters(PropertyInfo propertyInfo);

        public abstract string SchemaColumnQuery { get; }

        public abstract object SchemaColumnParameters(Type type);

        public bool IsTableEmpty(IDbConnection connection, Type t)
        {
            //$"SELECT COUNT(1) FROM [{schema}].[{tableName}]"
            return ((connection.QueryFirstOrDefault<int?>(IsTableEmptyQuery, null) ?? 0) == 0);
        }

        public bool TableExists(IDbConnection connection, Type t)
        {
            //return connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = schema, name = tableName });
            return connection.Exists(TableExistsQuery, TableExistsParameters(t));
        }

        public bool ColumnExists(IDbConnection connection, PropertyInfo pi)
        {
            return connection.Exists(ColumnExistsQuery, ColumnExistsParameters(pi));
        }

        public IEnumerable<ColumnInfo> GetSchemaColumns(IDbConnection connection, Type type)
        {
            var results = connection.Query<ColumnInfo>(SchemaColumnQuery, SchemaColumnParameters(type));
            // todo exclude select schemas
            return results;
        }

        public abstract IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo);

        public abstract Dictionary<Type, string> KeyTypeMap(bool withDefaults = true);
    }
}