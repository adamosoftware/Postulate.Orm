using Postulate.Orm.Attributes;
using ReflectionHelper;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Postulate.Orm.Models
{
    public class TableInfo
    {
        public TableInfo(string name, string schema = "", IDbConnection connection = null)
        {
            Schema = schema;
            Name = name;
            if (connection != null) ObjectId = FindObjectId(connection);
        }

        public TableInfo(string name, string schema, int objectId)
        {
            Schema = schema;
            Name = name;
            ObjectId = objectId;
        }

        public static TableInfo FromModelType(Type type, string defaultSchema = "", IDbConnection connection = null)
        {
            string schema = defaultSchema;
            string name = type.Name;

            if (type.HasAttribute(out SchemaAttribute schemaAttr)) schema = schemaAttr.Schema;

            if (type.HasAttribute(out TableAttribute tblAttr, a => !string.IsNullOrEmpty(a.Schema))) schema = tblAttr.Schema;
            if (type.HasAttribute(out tblAttr, a => !string.IsNullOrEmpty(a.Name))) name = tblAttr.Name;

            var result = new TableInfo(schema, name) { ModelType = type };

            if (connection != null) result.ObjectId = result.FindObjectId(connection);

            return result;
        }

        public string Schema { get; private set; }
        public string Name { get; private set; }
        public Type ModelType { get; private set; }
        public int ObjectId { get; private set; }

        public override string ToString()
        {
            return (string.IsNullOrEmpty(Schema)) ? Name : $"{Schema}.{Name}";
        }
    }
}