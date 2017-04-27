using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Data;
using Dapper;
using Postulate.Attributes;

namespace Postulate.Merge
{
	public class DbObject
	{
		private readonly string _schema;
		private readonly string _name;

		private const string _tempSuffix = "_temp";

		public DbObject(string schema, string name)
		{
			_schema = schema;
			_name = name;
			SquareBraces = true;
		}

		public DbObject()
		{
		}

		public string Schema { get { return _schema; } }
		public string Name { get { return _name; } }
		public int ObjectId { get; set; }
		public Type ModelType { get; set; }
		public bool SquareBraces { get; set; }

		public string QualifiedName()
		{
			return $"{Schema}.{Name}";
		}

		public override string ToString()
		{			
			return (SquareBraces) ? $"[{Schema}].[{Name}]" : $"{Schema}.{Name}";
		}

		public DbObject GetTemp()
		{
			return new DbObject(Schema, Name + _tempSuffix);
		}

		public static DbObject FromTempName(DbObject obj)
		{
			return new DbObject(obj.Schema, obj.Name.Substring(0, obj.Name.IndexOf(_tempSuffix)));
		}

		public override bool Equals(object obj)
		{
			DbObject test = obj as DbObject;
			if (test != null)
			{
				return test.Schema.ToLower().Equals(this.Schema.ToLower()) && test.Name.ToLower().Equals(this.Name.ToLower());
			}

			Type testType = obj as Type;
			if (testType != null) return Equals(FromType(testType));

			return false;
		}

		public override int GetHashCode()
		{
			return Schema.GetHashCode() + Name.GetHashCode();
		}

		public static DbObject FromType(Type modelType, IDbConnection connection)
		{
			DbObject obj = FromType(modelType);
			obj.ObjectId = connection.QueryFirstOrDefault<int>("SELECT [object_id] FROM [sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = obj.Schema, name = obj.Name });
			return obj;
		}

		public static DbObject FromType(Type modelType)
		{
            string schema, name;
            CreateTable.ParseNameAndSchema(modelType, out schema, out name);
			return new DbObject(schema, name) { ModelType = modelType };
		}

		public static string ConstraintName(Type modelType)
		{
			DbObject obj = FromType(modelType);
			string result = obj.Name;
			if (!obj.Schema.Equals("dbo")) result = obj.Schema.Substring(0, 1).ToUpper() + obj.Schema.Substring(1).ToLower() + result;
			return result;
		}

		public static string SqlServerName(Type modelType)
		{
			DbObject obj = FromType(modelType);
			obj.SquareBraces = true;
			return obj.ToString();
		}
	}
}
