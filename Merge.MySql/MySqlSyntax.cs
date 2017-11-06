using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge.MySql
{
    public class MySqlSyntax : SqlSyntax
    {
        public override string CommentPrefix => "# ";

        public override string CommandSeparator => ";\r\n\r\n";

        public override string IsTableEmptyQuery => throw new NotImplementedException();

        public override string TableExistsQuery => throw new NotImplementedException();

        public override string ColumnExistsQuery => throw new NotImplementedException();

        public override string SchemaColumnQuery => throw new NotImplementedException();

        public override string ApplyDelimiter(string objectName)
        {
            throw new NotImplementedException();
        }

        public override object ColumnExistsParameters(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override int FindObjectId(IDbConnection connection, TableInfo tableInfo)
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
    }
}