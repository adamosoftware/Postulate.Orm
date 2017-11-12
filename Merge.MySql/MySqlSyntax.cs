using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.MySql
{
    public class MySqlSyntax : SqlSyntax
    {
        public override string CommentPrefix => "# ";

        public override string CommandSeparator => ";\r\n\r\n";

        public override string ApplyDelimiter(string objectName)
        {
            return string.Join(".", objectName.Split('.').Select(s => $"`{s}`"));
        }        

        public override string TableExistsQuery => throw new NotImplementedException();

        public override string ColumnExistsQuery => throw new NotImplementedException();

        public override string SchemaColumnQuery => throw new NotImplementedException();

        public override string IndexExistsQuery => throw new NotImplementedException();        

        public override object ColumnExistsParameters(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override bool FindObjectId(IDbConnection connection, TableInfo tableInfo)
        {
            throw new NotImplementedException();
        }

        public override string GetColumnDefault(PropertyInfo propertyInfo, bool forCreateTable = false)
        {
            throw new NotImplementedException();
        }

        public override string GetColumnSyntax(PropertyInfo propertyInfo, bool forceNull = false)
        {
            throw new NotImplementedException();
        }

        public override string GetColumnType(PropertyInfo propertyInfo, bool forceNull = false)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo)
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

        public override string GetTableName(Type type)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<Type, string> KeyTypeMap(bool withDefaults = true)
        {
            throw new NotImplementedException();
        }

        public override object SchemaColumnParameters(Type type)
        {
            throw new NotImplementedException();
        }

        public override string SqlDataType(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<Type, string> SupportedTypes(string length = null, byte precision = 0, byte scale = 0)
        {
            throw new NotImplementedException();
        }

        public override object TableExistsParameters(Type type)
        {
            throw new NotImplementedException();
        }

        protected override string GetExcludeSchemas(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public override string GetConstraintBaseName(Type type)
        {
            throw new NotImplementedException();
        }

        public override string GetDropForeignKeyStatement(ForeignKeyInfo foreignKeyInfo)
        {
            throw new NotImplementedException();
        }

        public override string GetDropTableStatement(TableInfo tableInfo)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<KeyColumnInfo> GetKeyColumns(IDbConnection connection, Func<KeyColumnInfo, bool> filter = null)
        {
            throw new NotImplementedException();
        }

        public override string GetCopyStatement<TRecord, TKey>(IEnumerable<string> paramColumns, IEnumerable<string> columns)
        {
            throw new NotImplementedException();
        }

        public override string GetCreateTableStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
        {
            throw new NotImplementedException();
        }

        public override string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
        {
            throw new NotImplementedException();
        }

        public override string GetForeignKeyStatement(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string GetCreateColumnIndexStatement(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override TableInfo GetTableInfoFromType(Type type)
        {
            throw new NotImplementedException();
        }

        public override bool IsTableEmpty(IDbConnection connection, Type t)
        {
            throw new NotImplementedException();
        }

        public override string AddColumnStatement(TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false)
        {
            throw new NotImplementedException();
        }

        public override string AlterColumnStatement(TableInfo tableInfo, PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string UpdateColumnWithExpressionStatement(TableInfo tableInfo, PropertyInfo propertyInfo, string expression)
        {
            throw new NotImplementedException();
        }

        public override bool IsColumnInPrimaryKey(IDbConnection connection, ColumnInfo fromColumn, out string constraintName)
        {
            throw new NotImplementedException();
        }

        public override string AddPrimaryKeyStatement(TableInfo affectedTable)
        {
            throw new NotImplementedException();
        }

        public override string DropPrimaryKeyStatement(TableInfo affectedTable, string pkName)
        {
            throw new NotImplementedException();
        }
    }
}