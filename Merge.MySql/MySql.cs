using Postulate.Orm.Merge.Abstract;
using Postulate.Orm.Merge.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.MySql
{
    public class MySql : SqlScriptGenerator
    {
        public override string CommentPrefix => "# ";

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

        public override IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo)
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

        public override object TableExistsParameters(Type type)
        {
            throw new NotImplementedException();
        }
    }
}