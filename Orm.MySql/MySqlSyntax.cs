using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Postulate.Orm.Exceptions;
using System.Text;

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
            return new Dictionary<Type, string>()
            {
                { typeof(int), $"int {((withDefaults) ? "auto_increment" : string.Empty)}" }
            };
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
            return new Dictionary<Type, string>()
            {
                { typeof(bool), "boolean" },
                { typeof(byte), "tinyint" },
                { typeof(string), $"varchar({length})" },
                { typeof(int), "int" },
                { typeof(DateTime), "datetime" },
                { typeof(decimal), "decimal" },
                { typeof(Guid), "char(36)" },
                { typeof(long), "bigint" },
                { typeof(TimeSpan), "time" },
                { typeof(byte[]), $"varbinary({length})" },
                { typeof(Int16), "smallint" },
                { typeof(Single), "float" },
                { typeof(Double), "double" }
            };
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

        public override string ForeignKeyDropStatement(ForeignKeyInfo foreignKeyInfo)
        {
            throw new NotImplementedException();
        }

        public override string TableDropStatement(TableInfo tableInfo)
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

        public override string TableCreateStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
        {
            throw new NotImplementedException();
        }

        public override string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
        {
            throw new NotImplementedException();
        }

        public override string ForeignKeyAddStatement(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string CreateColumnIndexStatement(PropertyInfo propertyInfo)
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

        public override string ColumnAddStatement(TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false)
        {
            throw new NotImplementedException();
        }

        public override string ColumnAlterStatement(TableInfo tableInfo, PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override string UpdateColumnWithExpressionStatement(TableInfo tableInfo, PropertyInfo propertyInfo, string expression)
        {
            throw new NotImplementedException();
        }

        public override bool IsColumnInPrimaryKey(IDbConnection connection, ColumnInfo fromColumn, out bool clustered, out string constraintName)
        {
            throw new NotImplementedException();
        }

        public override string PrimaryKeyAddStatement(TableInfo affectedTable)
        {
            throw new NotImplementedException();
        }

        public override string PrimaryKeyDropStatement(TableInfo affectedTable, string pkName)
        {
            throw new NotImplementedException();
        }

        public override bool SchemaExists(IDbConnection connection, string schemaName)
        {
            throw new NotImplementedException();
        }

        public override string CreateSchemaStatement(string name)
        {
            throw new NotImplementedException();
        }

        public override string ForeignKeyAddStatement(ForeignKeyInfo foreignKeyInfo)
        {
            throw new NotImplementedException();
        }

        public override string ColumnDropStatement(ColumnInfo columnInfo)
        {
            throw new NotImplementedException();
        }

        public override string ApplyPaging(string sql, int pageNumber, int rowsPerPage)
        {
            throw new NotImplementedException();
        }

		public override string GetScriptFromSaveException(SaveException exception)
		{
			throw new NotImplementedException();			
		}

        public override string CreateEnumTableStatement(Type enumType)
        {
            throw new NotImplementedException();
        }

        public override string CheckEnumValueExistsStatement(string tableName)
        {
            throw new NotImplementedException();
        }

        public override string InsertEnumValueStatement(string tableName, string name, int value)
        {
            throw new NotImplementedException();
        }
    }
}