using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postulate.Orm.Models
{
	public class TableInfo
	{
		public TableInfo(string name, string schema = "", Type modelType = null)
		{
			Schema = schema;
			Name = name;
			ModelType = modelType;
		}

		public TableInfo(string name, string schema, int objectId, Type modelType = null)
		{
			Schema = schema;
			Name = name;
			ObjectId = objectId;
			ModelType = modelType;
		}

		public static TableInfo FromModelType(Type type, string defaultSchema = "")
		{
			EnumTableAttribute attr;
			if (type.IsEnum && type.HasAttribute(out attr))
			{
				return new TableInfo(attr.TableName, attr.Schema ?? defaultSchema);
			}

			string schema = defaultSchema;
			string name = type.Name;

			if (type.HasAttribute(out SchemaAttribute schemaAttr)) schema = schemaAttr.Schema;

			if (type.HasAttribute(out TableAttribute tblAttr, a => !string.IsNullOrEmpty(a.Schema))) schema = tblAttr.Schema;
			if (type.HasAttribute(out tblAttr, a => !string.IsNullOrEmpty(a.Name))) name = tblAttr.Name;

			var result = new TableInfo(name, schema) { ModelType = type };

			return result;
		}

		public string Schema { get; private set; }
		public string Name { get; private set; }
		public Type ModelType { get; private set; }

		public int ObjectId { get; set; }

		public override string ToString()
		{
			return (string.IsNullOrEmpty(Schema)) ? Name : $"{Schema}.{Name}";
		}

		public override int GetHashCode()
		{
			return (Schema?.GetHashCode() ?? 0) + Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			TableInfo tableCompare = obj as TableInfo;
			if (tableCompare != null)
			{
				return tableCompare.Schema.ToLower().Equals(this.Schema.ToLower()) && tableCompare.Name.ToLower().Equals(this.Name.ToLower());
			}

			return false;
		}
	}
}