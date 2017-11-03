using Postulate.Orm.Attributes;
using ReflectionHelper;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postulate.Orm.Merge.Models
{
    public class TableInfo
    {
        private readonly string _schema;
        private readonly string _name;

        public TableInfo(string schema, string name)
        {
            _schema = schema;
            _name = name;
        }

        public static TableInfo FromModelType(Type type, string defaultSchema = "")
        {
            string schema = defaultSchema;
            string name = type.Name;

            if (type.HasAttribute(out SchemaAttribute schemaAttr)) schema = schemaAttr.Schema;

            if (type.HasAttribute(out TableAttribute tblAttr, a => !string.IsNullOrEmpty(a.Schema))) schema = tblAttr.Schema;
            if (type.HasAttribute(out tblAttr, a => !string.IsNullOrEmpty(a.Name))) name = tblAttr.Name;

            return new TableInfo(schema, name) { ModelType = type };
        }

        public string Schema { get { return _schema; } }
        public string TableName { get { return _name; } }
        public Type ModelType { get; private set; }
    }
}