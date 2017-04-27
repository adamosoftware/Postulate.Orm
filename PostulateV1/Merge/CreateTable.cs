using Postulate.Attributes;
using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
    public class CreateTable : Diff
    {
        private readonly Type _modelType;
        private readonly string _schema;
        private readonly string _name;

        public CreateTable(Type modelType) : base(MergeObjectType.Table, MergeActionType.Create, $"Create table {TableName(modelType)}")
        {
            if (modelType.HasAttribute<NotMappedAttribute>()) throw new InvalidOperationException($"The model class {modelType.Name} is marked as [NotMapped]");

            _modelType = modelType;
            ParseNameAndSchema(modelType, out _schema, out _name);
        }

        public string Schema { get { return _schema; } }
        public string Name { get { return _name; } }

        public static string TableName(Type modelType)
        {
            string schema, name;
            ParseNameAndSchema(modelType, out schema, out name);
            return $"[{schema}].[{name}]";
        }

        public static void ParseNameAndSchema(Type modelType, out string schema, out string name)
        {
            schema = "dbo";
            name = modelType.Name;
            bool hasTableAttributeSchema = false;

            TableAttribute attr;
            if (modelType.HasAttribute(out attr))
            {
                if (!string.IsNullOrEmpty(attr.Name)) name = attr.Name;
                if (!string.IsNullOrEmpty(attr.Schema))
                {
                    hasTableAttributeSchema = true;
                    schema = attr.Schema;
                }
            }

            SchemaAttribute schemaAttr;
            if (modelType.HasAttribute(out schemaAttr))
            {
                if (hasTableAttributeSchema) throw new InvalidOperationException($"Model class {modelType.Name} may not have both [Table] and [Schema] attributes.");
                schema = schemaAttr.Schema;
            }
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            if (_modelType.GetProperties().Any(pi => pi.HasAttribute<UniqueKeyAttribute>(attr => attr.IsClustered)) && _modelType.HasAttribute<ClusterAttribute>())
            {
                yield return "Model class with [Cluster] attribute may not have properties with a clustered unique key.";
            }

            foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<PrimaryKeyAttribute>())))
            {
                if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Primary key column [{pi.Name}] may not use MAX size.";
                if (pi.PropertyType.IsNullableGeneric()) yield return $"Primary key column [{pi.Name}] may not be nullable.";
            }

            foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<UniqueKeyAttribute>())))
            {
                if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
            }

            // class-level unique with MAX
            var uniques = _modelType.GetCustomAttributes<UniqueKeyAttribute>();
            foreach (var u in uniques)
            {
                foreach (var col in u.ColumnNames)
                {
                    PropertyInfo pi = _modelType.GetProperty(col);
                    if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
                }
            }
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            yield return
                $"CREATE TABLE {TableName(_modelType)} (\r\n\t" +
                    string.Join(",\r\n\t", CreateTableMembers(false)) +
                "\r\n)";
        }

        private string[] CreateTableMembers(bool withForeignKeys = false)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            Type t = obj as Type;
            if (t != null)
            {
                string schema, name;
                ParseNameAndSchema(t, out schema, out name);
                return Schema.Equals(schema) && Name.Equals(name);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
