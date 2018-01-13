using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Postulate.Orm.Abstract
{
	/// <summary>
	/// Base class for defining SQL dialect rules that apply to a specific database platform
	/// </summary>
	public abstract class SqlSyntax
	{
		public abstract IDbConnection GetConnection(string connectionString);		

		/// <summary>
		/// Indicates whether schema objects (in the SQL Server sense) are supported
		/// </summary>
		public abstract bool SupportsSchemas { get; }

		public void CreateTable<TModel>(IDbConnection connection, bool withForeignkeys = false) where TModel : Record<int>
		{
			var create = new ModelMerge.Actions.CreateTable(this, this.GetTableInfoFromType(typeof(TModel)), false, withForeignkeys);
			foreach (var cmd in create.SqlCommands(connection)) connection.Execute(cmd);
		}

		public string CreateTableScript<TModel>(IDbConnection connection, bool withForeignKeys = false) where TModel : Record<int>
		{
			var create = new ModelMerge.Actions.CreateTable(this, this.GetTableInfoFromType(typeof(TModel)), withForeignKeys);
			StringBuilder sb = new StringBuilder();
			foreach (var cmd in create.SqlCommands(connection))
			{
				sb.Append(cmd);
				sb.Append(CommandSeparator);
			}
			return sb.ToString();
		}

		public abstract string CommentPrefix { get; }

		public abstract string CommandSeparator { get; }

		/// <summary>
		/// Applies the platform-specific delimiter around object names, such as square brackets around SQL Server object names or backticks in MySQL
		/// </summary>
		public abstract string ApplyDelimiter(string objectName);

		/// <summary>
		/// Returns the column name followed by the alias, if specified. Used by <see cref="SqlDb{TKey}.Find{TRecord}(TKey)"/> method to ensure column and property names are mapped
		/// </summary>
		public string SelectExpression(PropertyInfo propertyInfo)
		{
			string result = ApplyDelimiter(propertyInfo.SqlColumnName());

			ColumnAttribute colAttr;
			if (propertyInfo.HasAttribute(out colAttr, a => !string.IsNullOrEmpty(a.Name)))
			{
				result += " AS " + ApplyDelimiter(propertyInfo.Name);
			}

			return result;
		}

		/// <summary>
		/// Returns the database table name for the given model type, applying the schema name, if specified with the [Table] or [Schema] attribute
		/// </summary>
		public abstract string GetTableName(Type type);

		public abstract string TableExistsQuery { get; }

		public abstract object TableExistsParameters(Type type);

		public abstract string ColumnExistsQuery { get; }

		public abstract object ColumnExistsParameters(PropertyInfo propertyInfo);

		public abstract string IndexExistsQuery { get; }

		public abstract string SchemaColumnQuery { get; }

		public abstract object SchemaColumnParameters(Type type);

		public abstract bool IsColumnInPrimaryKey(IDbConnection connection, ColumnInfo columnInfo, out bool clustered, out string constraintName);

		public abstract bool FindObjectId(IDbConnection connection, TableInfo tableInfo);

		public virtual string SqlDataType(PropertyInfo propertyInfo)
		{
			string result = null;

			ColumnAttribute colAttr;
			if (propertyInfo.HasAttribute(out colAttr))
			{
				return colAttr.TypeName;
			}
			else
			{
				string length = "max";
				var maxLenAttr = propertyInfo.GetCustomAttribute<MaxLengthAttribute>();
				if (maxLenAttr != null) length = maxLenAttr.Length.ToString();

				byte precision = 5, scale = 2; // some aribtrary defaults
				var dec = propertyInfo.GetCustomAttribute<DecimalPrecisionAttribute>();
				if (dec != null)
				{
					precision = dec.Precision;
					scale = dec.Scale;
				}

				var typeMap = SupportedTypes(length, precision, scale);

				Type t = propertyInfo.PropertyType;
				if (t.IsGenericType) t = t.GenericTypeArguments[0];
				if (t.IsEnum) t = t.GetEnumUnderlyingType();

				if (!typeMap.ContainsKey(t)) throw new KeyNotFoundException($"Type name {t.Name} not supported.");

				result = typeMap[t];
			}

			return result;
		}

		public abstract bool IsTableEmpty(IDbConnection connection, Type t);

		public bool TableExists(IDbConnection connection, Type t)
		{
			return connection.Exists(TableExistsQuery, TableExistsParameters(t));
		}

		public abstract bool SchemaExists(IDbConnection connection, string schemaName);

		public bool ColumnExists(IDbConnection connection, PropertyInfo pi)
		{
			return connection.Exists(ColumnExistsQuery, ColumnExistsParameters(pi));
		}

		public bool IndexExists(IDbConnection connection, string name)
		{
			return connection.Exists(IndexExistsQuery, new { name = name });
		}

		public abstract string ColumnAddStatement(TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false);

		public abstract string ColumnDropStatement(ColumnInfo columnInfo);

		public abstract string PrimaryKeyAddStatement(TableInfo affectedTable);

		public abstract string PrimaryKeyDropStatement(TableInfo affectedTable, string pkName);

		public abstract string ColumnAlterStatement(TableInfo tableInfo, PropertyInfo propertyInfo);

		public abstract string UpdateColumnWithExpressionStatement(TableInfo tableInfo, PropertyInfo propertyInfo, string expression);

		public abstract ILookup<int, ColumnInfo> GetSchemaColumns(IDbConnection connection);

		public abstract IEnumerable<TableInfo> GetSchemaTables(IDbConnection connection);

		protected abstract string GetExcludeSchemas(IDbConnection connection);

		public abstract IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo);

		public abstract Dictionary<Type, string> KeyTypeMap(bool withDefaults = true);

		public abstract Dictionary<Type, string> SupportedTypes(string length = null, byte precision = 0, byte scale = 0);

		public bool IsSupportedType(Type type)
		{
			return
			   SupportedTypes().ContainsKey(type) ||
			   (type.IsEnum && type.GetEnumUnderlyingType().Equals(typeof(int))) ||
			   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSupportedType(type.GetGenericArguments()[0]));
		}

		public abstract string GetColumnSyntax(PropertyInfo propertyInfo, bool forceNull = false);

		public abstract string GetColumnType(PropertyInfo propertyInfo, bool forceNull = false);

		public abstract string GetColumnDefault(PropertyInfo propertyInfo, bool forCreateTable = false);

		public abstract string GetConstraintBaseName(Type type);

		public abstract string ForeignKeyDropStatement(ForeignKeyInfo foreignKeyInfo);

		public abstract IEnumerable<TableInfo> GetTables(IDbConnection connection);

		public abstract IEnumerable<ColumnInfo> GetColumns(IDbConnection connection);

		public abstract string TableDropStatement(TableInfo tableInfo);

		public abstract string TableCreateStatement(IDbConnection connection, TableInfo tableInfo);

		public string TableCreateStatement(Type type, bool withForeignKeys = false)
		{
			return TableCreateStatement(type, null, null, null, withForeignKeys);
		}

		public abstract string TableCreateStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns, bool withForeignKeys = false);

		public abstract string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns, bool withForeignKeys = false);

		public abstract IEnumerable<KeyColumnInfo> GetKeyColumns(IDbConnection connection, Func<KeyColumnInfo, bool> filter = null);

		public abstract string GetCopyStatement<TRecord, TKey>(IEnumerable<string> paramColumns, IEnumerable<string> columns) where TRecord : Record<TKey>;

		public abstract bool ForeignKeyExists(IDbConnection connection, PropertyInfo propertyInfo);
		
		public abstract string ForeignKeyAddStatement(ForeignKeyInfo foreignKeyInfo);

		public abstract string ForeignKeyAddStatement(PropertyInfo propertyInfo);

		public virtual string ForeignKeyConstraintSyntax(PropertyInfo propertyInfo)
		{
			Attributes.ForeignKeyAttribute fk = propertyInfo.GetForeignKeyAttribute();
			string cascadeDelete = (fk?.CascadeDelete ?? false) ? " ON DELETE CASCADE" : string.Empty;
			string firstLine = $"CONSTRAINT {ApplyDelimiter(propertyInfo.ForeignKeyName(this))} FOREIGN KEY (\r\n";

			EnumTableAttribute enumTable;
			if (propertyInfo.IsEnumForeignKey(out enumTable))
			{				
				return
					firstLine +
						$"\t{ApplyDelimiter(propertyInfo.SqlColumnName())}\r\n" +
					$") REFERENCES {ApplyDelimiter(enumTable.FullTableName())} (\r\n" +
						$"\t{ApplyDelimiter("Value")}\r\n" +
					")";
			}
			else
			{
				return
					firstLine +
						$"\t{ApplyDelimiter(propertyInfo.SqlColumnName())}\r\n" +
					$") REFERENCES {GetTableName(fk.PrimaryTableType)} (\r\n" +
						$"\t{ApplyDelimiter(fk.PrimaryTableType.IdentityColumnName())}\r\n" +
					")" + cascadeDelete;
			}
		}

		public virtual string ForeignKeyConstraintSyntax(ForeignKeyInfo foreignKeyInfo)
		{
			string cascadeDelete = (foreignKeyInfo.CascadeDelete) ? " ON DELETE CASCADE" : string.Empty;
			return
				$"CONSTRAINT {ApplyDelimiter(foreignKeyInfo.ConstraintName)} FOREIGN KEY(\r\n" +
					$"\t{ApplyDelimiter(foreignKeyInfo.Child.ColumnName)}\r\n" +
				$") REFERENCES {ApplyDelimiter(foreignKeyInfo.Parent.Schema)}.{ApplyDelimiter(foreignKeyInfo.Parent.TableName)} (\r\n" +
					$"\t{ApplyDelimiter(foreignKeyInfo.Parent.ColumnName)}\r\n" +
				$")" + cascadeDelete;
		}

		public abstract string CreateColumnIndexStatement(PropertyInfo propertyInfo);

		public abstract string CreateSchemaStatement(string name);

		public abstract TableInfo GetTableInfoFromType(Type type);

		public static IEnumerable<PropertyInfo> PrimaryKeyProperties(Type type, bool markedOnly = false)
		{
			var pkProperties = type.GetProperties().Where(pi => pi.HasAttribute<PrimaryKeyAttribute>());
			if (pkProperties.Any() || markedOnly) return pkProperties;
			return new PropertyInfo[] { type.GetProperty(type.IdentityColumnName()) };
		}

		public static IEnumerable<string> PrimaryKeyColumns(Type type, bool markedOnly = false)
		{
			return PrimaryKeyProperties(type, markedOnly).Select(pi => pi.SqlColumnName());
		}

		public abstract string ApplyPaging(string sql, int pageNumber, int rowsPerPage);

		/// <summary>
		/// Reproduces the script from a failed statement execution so you can troubleshoot in your SQL tool of choice
		/// </summary>
		public abstract string GetScriptFromSaveException(SaveException exception);

		public abstract string CreateEnumTableStatement(Type enumType);

		public abstract string CheckEnumValueExistsStatement(string tableName);

		public abstract string InsertEnumValueStatement(string tableName, string name, int value);

		protected string IdentityColumnSql(Type type)
		{
			Type keyType = FindKeyType(type);

			return $"{ApplyDelimiter(type.IdentityColumnName())} {KeyTypeMap()[keyType]}";
		}

		protected Type FindKeyType(Type modelType)
		{
			if (!modelType.IsDerivedFromGeneric(typeof(Record<>))) throw new ArgumentException("Model class must derive from Record<TKey>");

			Type checkType = modelType;
			while (!checkType.IsGenericType) checkType = checkType.BaseType;
			return checkType.GetGenericArguments()[0];
		}

		public IEnumerable<PropertyInfo> ColumnProperties(Type type)
		{
			return type.GetProperties()
				.Where(p =>
					p.CanWrite &&
					!p.Name.ToLower().Equals(type.IdentityColumnName().ToLower()) &&
					IsSupportedType(p.PropertyType) &&					
					!p.HasAttribute<NotMappedAttribute>());
		}
	}
}