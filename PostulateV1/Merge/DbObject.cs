using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Data;
using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge.Action;
using Postulate.Orm.Extensions;
using System.Collections.Generic;

namespace Postulate.Orm.Merge
{
	public class DbObject
	{
		private readonly string _schema;
		private readonly string _name;

		private const string _tempSuffix = "_temp";

		public DbObject(string schema, string name, int objectId = 0)
		{
			_schema = schema;
			_name = name;
            ObjectId = objectId;
			SquareBraces = true;
            PKConstraintName = "PK_" + ConstraintName(schema, name);
        }

        public DbObject(string schema, string name, IDbConnection connection)
        {
            _schema = schema;
            _name = name;
            SquareBraces = true;
            PKConstraintName = "PK_" + ConstraintName(schema, name);
            SetObjectId(connection, this);
        }

		public DbObject()
		{
		}

        public static DbObject Parse(string objectName, IDbConnection connection = null)
        {
            DbObject result = null;
            string[] parts = objectName.Split('.');
            switch (parts.Length)
            {
                case 1:
                    result = new DbObject("dbo", objectName);
                    break;

                case 2:
                    result = new DbObject(parts[0], parts[1]);
                    break;

                default:
                    throw new InvalidOperationException($"Too many name parts in {objectName}");
            }

            if (connection != null) SetObjectId(connection, result);

            return result;
        }

        public static DbObject FromType(Type modelType, IDbConnection connection)
        {
            DbObject obj = FromType(modelType);
            SetObjectId(connection, obj);
            return obj;
        }

        public static void SetObjectId(IDbConnection connection, DbObject obj)
        {
            obj.ObjectId = connection.QueryFirstOrDefault<int>("SELECT [object_id] FROM [sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = obj.Schema, name = obj.Name });
        }

        public string Schema { get { return _schema; } }
		public string Name { get { return _name; } }
		public int ObjectId { get; set; }
		public Type ModelType { get; set; }
		public bool SquareBraces { get; set; }
        public bool IsClusteredPK { get; set; }
        public string PKConstraintName { get; set; }

		public string QualifiedName()
		{
			return $"{Schema}.{Name}";
		}

        public bool IsEmpty(IDbConnection connection)
        {
            return connection.IsTableEmpty(Schema, Name);
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

            RenameFromAttribute rename = obj as RenameFromAttribute;
            if (rename != null)
            {
                DbObject renamedObj = Parse(rename.OldName);
                return renamedObj.Equals(this);
            }

			return false;
		}

		public override int GetHashCode()
		{
			return Schema.GetHashCode() + Name.GetHashCode();
		}

		public static DbObject FromType(Type modelType)
		{
            string schema, name;
            CreateTable.ParseNameAndSchema(modelType, out schema, out name);
			return new DbObject(schema, name)
                {
                    ModelType = modelType,
                    IsClusteredPK = modelType.HasClusteredPrimaryKey()                    
                };
		}

		public static string ConstraintName(Type modelType)
		{
			DbObject obj = FromType(modelType);
            return obj.ConstraintName();
		}

        public static string ConstraintName(string schema, string name)
        {
            string result = name;
            if (!schema.Equals("dbo")) result = schema.Substring(0, 1).ToUpper() + schema.Substring(1).ToLower() + result;
            return result;
        }

        public string ConstraintName()
        {
            return ConstraintName(this.Schema, this.Name);
        }

		public static string SqlServerName(Type modelType, bool squareBrackets = true)
		{
			DbObject obj = FromType(modelType);
			obj.SquareBraces = squareBrackets;
			return obj.ToString();
		}

        internal IEnumerable<ForeignKeyRef> GetReferencingForeignKeys(IDbConnection connection)
        {
            if (ObjectId == 0) SetObjectId(connection, this);
            return connection.GetReferencingForeignKeys(ObjectId);
        }
    }
}
