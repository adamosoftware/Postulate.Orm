using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.ModelMerge.Actions;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.SqlServer
{
	public partial class SqlServerSyntax : SqlSyntax
	{
		public override IDbConnection GetConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}

		public override bool SupportsSchemas => true;

		public override string CommentPrefix => "-- ";

		public override string CommandSeparator => "\r\nGO\r\n";

		public override string ApplyDelimiter(string name)
		{
			return string.Join(".", name.Split('.').Select(s => $"[{s}]"));
		}

		public override string TableExistsQuery =>
			"[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name";

		public override string ColumnExistsQuery =>
			@"[sys].[columns] [col] INNER JOIN [sys].[tables] [tbl] ON [col].[object_id]=[tbl].[object_id]
			WHERE SCHEMA_NAME([tbl].[schema_id])=@schema AND [tbl].[name]=@tableName AND [col].[name]=@columnName";

		public override bool SchemaExists(IDbConnection connection, string schemaName)
		{
			return connection.Exists("[sys].[schemas] WHERE [name]=@name", new { name = schemaName });
		}

		public override object ColumnExistsParameters(PropertyInfo propertyInfo)
		{
			return ColumnInfo.FromPropertyInfo(propertyInfo, this);
		}

		public override bool IsTableEmpty(IDbConnection connection, Type type)
		{
			var obj = GetTableInfoFromType(type);
			return ((connection.QueryFirstOrDefault<int?>($"SELECT COUNT(1) FROM [{obj.Schema}].[{obj.Name}]", null) ?? 0) == 0);
		}

		public override string IndexExistsQuery => "[sys].[indexes] WHERE [name]=@name";

		public override string SchemaColumnQuery => throw new NotImplementedException();

		public override IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo)
		{
			return connection.Query<ForeignKeyData>(
				@"SELECT
                    [fk].[name] AS [ConstraintName],
                    SCHEMA_NAME([parent].[schema_id]) AS [ReferencedSchema],
                    [parent].[name] AS [ReferencedTable],
                    [refdcol].[name] AS [ReferencedColumn],
                    SCHEMA_NAME([child].[schema_id]) AS [ReferencingSchema],
                    [child].[name] AS [ReferencingTable],
                    [rfincol].[name] AS [ReferencingColumn],
					CONVERT(bit, CASE [fk].[delete_referential_action] WHEN 1 THEN 1 ELSE 0 END) AS [CascadeDelete]
                FROM
                    [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [child] ON [fk].[parent_object_id]=[child].[object_id]
                    INNER JOIN [sys].[tables] [parent] ON [fk].[referenced_object_id]=[parent].[object_id]
                    INNER JOIN [sys].[foreign_key_columns] [fkcol] ON
                        [fk].[parent_object_id]=[fkcol].[parent_object_id] AND
                        [fk].[object_id]=[fkcol].[constraint_object_id]
                    INNER JOIN [sys].[columns] [refdcol] ON
                        [fkcol].[referenced_column_id]=[refdcol].[column_id] AND
                        [fkcol].[referenced_object_id]=[refdcol].[object_id]
                    INNER JOIN [sys].[columns] [rfincol] ON
                        [fkcol].[parent_column_id]=[rfincol].[column_id] AND
                        [fkcol].[parent_object_id]=[rfincol].[object_id]
				WHERE
                    [fk].[referenced_object_id]=OBJECT_ID(@schema+'.' + @table)", new { schema = tableInfo.Schema, table = tableInfo.Name })
				.Select(fk => new ForeignKeyInfo()
				{
					ConstraintName = fk.ConstraintName,
					Child = new ColumnInfo() { Schema = fk.ReferencingSchema, TableName = fk.ReferencingTable, ColumnName = fk.ReferencingColumn },
					Parent = new ColumnInfo() { Schema = fk.ReferencedSchema, TableName = fk.ReferencedTable, ColumnName = fk.ReferencedColumn },
					CascadeDelete = fk.CascadeDelete
				});
		}

		public override string GetTableName(Type type)
		{
			var tableInfo = GetTableInfoFromType(type);
			return $"[{tableInfo.Schema}].[{tableInfo.Name}]";
		}

		public override object SchemaColumnParameters(Type type)
		{
			throw new NotImplementedException();
		}

		public override object TableExistsParameters(Type type)
		{
			return GetTableInfoFromType(type);
		}

		public override Dictionary<Type, string> KeyTypeMap(bool withDefaults = true)
		{
			return new Dictionary<Type, string>()
			{
				{ typeof(int), $"int{((withDefaults) ? " identity(1,1)" : string.Empty)}" },
				{ typeof(long), $"bigint{((withDefaults) ? " identity(1,1)" : string.Empty)}" },
				{ typeof(Guid), $"uniqueidentifier{((withDefaults) ? " DEFAULT NewSequentialID()" : string.Empty)}" }
			};
		}

		public override IEnumerable<TableInfo> GetSchemaTables(IDbConnection connection)
		{
			string excludeSchemas = GetExcludeSchemas(connection);

			var tables = connection.Query(
				$@"SELECT
                    SCHEMA_NAME([t].[schema_id]) AS [Schema], [t].[name] AS [Name], [t].[object_id] AS [ObjectId]
                FROM
                    [sys].[tables] [t]
                WHERE
                    SCHEMA_NAME([t].[schema_id]) NOT IN ('changes', 'meta', 'deleted'{excludeSchemas}) AND
                    [name] NOT LIKE 'AspNet%' AND
                    [name] NOT LIKE '__MigrationHistory'");

			return tables.Select(item => new TableInfo(item.Name, item.Schema, item.ObjectId));
		}

		protected override string GetExcludeSchemas(IDbConnection connection)
		{
			try
			{
				var schemas = connection.Query<string>("SELECT [Name] FROM [meta].[MergeExcludeSchema]");
				if (schemas.Any())
				{
					return ", " + string.Join(", ", schemas.Select(s => $"'{s.Trim()}'"));
				}
				return null;
			}
			catch
			{
				return null;
			}
		}

		public override ILookup<int, ColumnInfo> GetSchemaColumns(IDbConnection connection)
		{
			string excludeSchemas = GetExcludeSchemas(connection);

			return connection.Query<ColumnInfo>(
				$@"SELECT
	                SCHEMA_NAME([t].[schema_id]) AS [Schema], [t].[name] AS [TableName], [c].[Name] AS [ColumnName],
	                [t].[object_id] AS [ObjectID], TYPE_NAME([c].[system_type_id]) AS [DataType],
	                [c].[max_length] AS [ByteLength], [c].[is_nullable] AS [IsNullable],
	                [c].[precision] AS [Precision], [c].[scale] as [Scale], [c].[collation_name] AS [Collation], [c].[is_computed] AS [IsCalculated],
	                SCHEMA_NAME([parentTbl].[schema_id]) AS [ReferencedSchema], [parentTbl].[name] AS [ReferencedTable], [parentCol].[name] AS [ReferencedColumn],
	                [fk].[name] AS [ForeignKeyConstraint]
                FROM
	                [sys].[tables] [t] INNER JOIN [sys].[columns] [c] ON [t].[object_id]=[c].[object_id]
	                LEFT JOIN [sys].[foreign_key_columns] [fkcol] ON
		                [c].[object_id]=[fkcol].[parent_object_id] AND
		                [c].[column_id]=[fkcol].[parent_column_id]
                    LEFT JOIN [sys].[foreign_keys] [fk] ON [fkcol].[constraint_object_id]=[fk].[object_id]
	                LEFT JOIN [sys].[columns] [parentCol] ON
		                [fkcol].[referenced_object_id]=[parentCol].[object_id] AND
		                [fkcol].[referenced_column_id]=[parentCol].[column_id]
	                LEFT JOIN [sys].[tables] [parentTbl] ON
		                [parentCol].[object_id]=[parentTbl].[object_id]
                    WHERE
                        SCHEMA_NAME([t].[schema_id]) NOT IN ('changes', 'meta', 'deleted'{excludeSchemas}) AND
                        ([t].[name] NOT LIKE 'AspNet%' OR [t].[name] LIKE 'AspNetUsers') AND
                        [t].[name] NOT LIKE '__MigrationHistory'").ToLookup(item => item.ObjectId);
		}

		public override Dictionary<Type, string> SupportedTypes(string length = null, byte precision = 0, byte scale = 0)
		{
			return new Dictionary<Type, string>()
			{
				{ typeof(string), $"nvarchar({length})" },
				{ typeof(bool), "bit" },
				{ typeof(int), "int" },
				{ typeof(decimal), $"decimal({precision}, {scale})" },
				{ typeof(double), "float" },
				{ typeof(float), "float" },
				{ typeof(long), "bigint" },
				{ typeof(short), "smallint" },
				{ typeof(byte), "tinyint" },
				{ typeof(Guid), "uniqueidentifier" },
				{ typeof(DateTime), "datetime" },
				{ typeof(TimeSpan), "time" },
				{ typeof(char), "nchar(1)" },
				{ typeof(byte[]), $"varbinary({length})" }
			};
		}

		public override bool FindObjectId(IDbConnection connection, TableInfo tableInfo)
		{
			tableInfo.ObjectId = connection.QueryFirstOrDefault<int>("SELECT [object_id] FROM [sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = tableInfo.Schema, name = tableInfo.Name });
			return (tableInfo.ObjectId != 0);
		}

		public override string GetColumnSyntax(PropertyInfo propertyInfo, bool forceNull = false)
		{
			string result = null;

			CalculatedAttribute calc;
			if (propertyInfo.HasAttribute(out calc))
			{
				result = $"[{propertyInfo.SqlColumnName()}] AS {calc.Expression}";
				if (calc.IsPersistent) result += " PERSISTED";
			}
			else if (AddColumn.IsIdentityColumn(propertyInfo))
			{
				result = $"{IdentityColumnSql(propertyInfo.DeclaringType)} NOT NULL";
			}
			else
			{
				if (!forceNull)
				{
					result = $"[{propertyInfo.SqlColumnName()}] {GetColumnType(propertyInfo)}{GetColumnDefault(propertyInfo, forCreateTable: true)}";
				}
				else
				{
					// forced nulls are used with AddColumn where DefaultExpression.IsConstant = false. The null constraint is added after expression is resolved
					result = $"[{propertyInfo.SqlColumnName()}] {GetColumnType(propertyInfo, forceNull: true)}";
				}
			}

			return result;
		}

		public override string GetColumnType(PropertyInfo propertyInfo, bool forceNull = false)
		{
			string nullable = ((propertyInfo.AllowSqlNull() || forceNull) ? "NULL" : "NOT NULL");

			string result = SqlDataType(propertyInfo);

			CollateAttribute collate;
			string collation = string.Empty;
			if (propertyInfo.HasAttribute(out collate)) collation = $"COLLATE {collate.Collation} ";

			return $"{result} {collation}{nullable}";
		}

		public override string GetColumnDefault(PropertyInfo propertyInfo, bool forCreateTable = false)
		{
			string template = (forCreateTable) ? " DEFAULT ({0})" : "{0}";

			DefaultExpressionAttribute def;
			if (propertyInfo.DeclaringType.HasAttribute(out def) && def.IsConstant && propertyInfo.Name.Equals(def.ColumnName)) return string.Format(template, Quote(propertyInfo, def.Expression));
			if (propertyInfo.HasAttribute(out def) && def.IsConstant) return string.Format(template, Quote(propertyInfo, def.Expression));

			// if the expression is part of a CREATE TABLE statement, it's not necessary to go any further
			if (forCreateTable) return null;

			if (propertyInfo.AllowSqlNull()) return "NULL";

			throw new Exception($"{propertyInfo.DeclaringType.Name}.{propertyInfo.Name} property does not have a [DefaultExpression] attribute.");
		}

		private static string Quote(PropertyInfo propertyInfo, string expression)
		{
			string result = expression;

			var quotedTypes = new Type[] { typeof(string), typeof(DateTime) };
			if (quotedTypes.Any(t => t.Equals(propertyInfo.PropertyType)))
			{
				if (result.Contains("'") && !result.StartsWith("'") && !result.EndsWith("'")) result = result.Replace("'", "''");
				if (!result.StartsWith("'")) result = "'" + result;
				if (!result.EndsWith("'")) result = result + "'";
			}

			return result;
		}

		public override string GetConstraintBaseName(Type type)
		{
			var obj = GetTableInfoFromType(type);
			return (obj.Schema.ToLower().Equals("dbo")) ? obj.Name : TitleCase(obj.Schema) + obj.Name;
		}

		public override string ForeignKeyDropStatement(ForeignKeyInfo foreignKeyInfo)
		{
			return $"ALTER TABLE [{foreignKeyInfo.Child.Schema}].[{foreignKeyInfo.Child.TableName}] DROP CONSTRAINT [{foreignKeyInfo.ConstraintName}]";
		}

		public override string TableDropStatement(TableInfo tableInfo)
		{
			return $"DROP TABLE [{tableInfo.Schema}].[{tableInfo.Name}]";
		}

		public override IEnumerable<KeyColumnInfo> GetKeyColumns(IDbConnection connection, Func<KeyColumnInfo, bool> filter = null)
		{
			var results = connection.Query<KeyColumnInfo>(
				@"SELECT
                    SCHEMA_NAME([t].[schema_id]) AS [Schema],
                    [t].[name] AS [TableName],
                    [col].[name] AS [ColumnName],
                    [ndx].[name] AS [IndexName],
                    [ndx].[type] AS [IndexType],
                    [ndx].[is_unique] AS [IsUnique],
                    [ndx].[is_primary_key] AS [IsPrimaryKey]
                FROM
                    [sys].[index_columns] [ndxcol] INNER JOIN [sys].[indexes] [ndx] ON
                        [ndxcol].[object_id]=[ndx].[object_id] AND
                        [ndxcol].[index_id]=[ndx].[index_id]
                    INNER JOIN [sys].[tables] [t] ON [ndx].[object_id]=[t].[object_id]
                    INNER JOIN [sys].[columns] [col] ON
                         [ndxcol].[object_id]=[col].[object_id] AND
                         [ndxcol].[column_id]=[col].[column_id]");

			return (filter == null) ?
				results :
				results.Where(row => filter.Invoke(row));
		}

		private string TitleCase(string input)
		{
			if (input.Length >= 2)
			{
				return input.Substring(0, 1).ToUpper() + input.Substring(1).ToLower();
			}

			return input.ToUpper();
		}

		public override string GetCopyStatement<TRecord, TKey>(IEnumerable<string> paramColumns, IEnumerable<string> columns)
		{
			string tableName = GetTableName(typeof(TRecord));
			return
				$@"INSERT INTO {tableName} (
                    {string.Join(", ", columns.Concat(paramColumns.Select(col => ApplyDelimiter(col))))}
                ) OUTPUT
                    [inserted].[{typeof(TRecord).IdentityColumnName()}]
                SELECT
                    {string.Join(", ", columns.Concat(paramColumns.Select(col => $"@{col}")))}
                FROM
                    {tableName}
                WHERE
                    {ApplyDelimiter(typeof(TRecord).IdentityColumnName())}=@id";
		}

		public override string ForeignKeyAddStatement(ForeignKeyInfo foreignKeyInfo)
		{
			return $"ALTER TABLE [{foreignKeyInfo.Child.Schema}].[{foreignKeyInfo.Child.TableName}] " + ForeignKeyConstraintSyntax(foreignKeyInfo);
		}

		public override string CreateColumnIndexStatement(PropertyInfo propertyInfo)
		{
			var obj = TableInfo.FromModelType(propertyInfo.DeclaringType);
			return $"CREATE INDEX [{propertyInfo.IndexName(this)}] ON {GetTableName(obj.ModelType)} ([{propertyInfo.SqlColumnName()}])";
		}

		public override TableInfo GetTableInfoFromType(Type type)
		{
			return TableInfo.FromModelType(type, "dbo");
		}

		public override string ColumnAddStatement(TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false)
		{
			return ColumnStatementInner("ADD", tableInfo, propertyInfo, forceNull);
		}

		public override string ColumnAlterStatement(TableInfo tableInfo, PropertyInfo propertyInfo)
		{
			return ColumnStatementInner("ALTER COLUMN", tableInfo, propertyInfo, false);
		}

		private string ColumnStatementInner(string action, TableInfo tableInfo, PropertyInfo propertyInfo, bool forceNull = false)
		{
			return $"ALTER TABLE [{tableInfo.Schema}].[{tableInfo.Name}] {action} {GetColumnSyntax(propertyInfo, forceNull)}";
		}

		public override string ColumnDropStatement(ColumnInfo columnInfo)
		{
			return $"ALTER TABLE [{columnInfo.Schema}].[{columnInfo.TableName}] DROP COLUMN [{columnInfo.ColumnName}]";
		}

		public override string UpdateColumnWithExpressionStatement(TableInfo tableInfo, PropertyInfo propertyInfo, string expression)
		{
			return $"UPDATE [{tableInfo.Schema}].[{tableInfo.Name}] SET [{propertyInfo.SqlColumnName()}]={expression}";
		}

		public override string PrimaryKeyAddStatement(TableInfo affectedTable)
		{
			ClusterAttribute clusterAttribute = GetClusterAttribute(affectedTable.ModelType);
			return $"ALTER TABLE [{affectedTable.Schema}].[{affectedTable.Name}] ADD CONSTRAINT {CreateTablePrimaryKey(affectedTable.ModelType, clusterAttribute)}";
		}

		public override string PrimaryKeyDropStatement(TableInfo affectedTable, string pkName)
		{
			return $"ALTER TABLE [{affectedTable.Schema}].[{affectedTable.Name}] DROP CONSTRAINT [{pkName}]";
		}

		public override bool IsColumnInPrimaryKey(IDbConnection connection, ColumnInfo columnInfo, out bool clustered, out string constraintName)
		{
			constraintName = null;
			clustered = false;

			var result = connection.QueryFirstOrDefault(
				@"SELECT
					[ndx].[name] AS [ConstraintName], CONVERT(bit, CASE [ndx].[type_desc] WHEN 'CLUSTERED' THEN 1 ELSE 0 END) AS [IsClustered]
				FROM
					[sys].[indexes] [ndx] INNER JOIN [sys].[index_columns] [ndxcol] ON [ndx].[object_id]=[ndxcol].[object_id]
					INNER JOIN [sys].[columns] [col] ON
						[ndxcol].[column_id]=[col].[column_id] AND
						[ndxcol].[object_id]=[col].[object_id]
					INNER JOIN [sys].[tables] [t] ON [col].[object_id]=[t].[object_id]
				WHERE
					[is_primary_key]=1 AND
					SCHEMA_NAME([t].[schema_id])=@schema AND
					[t].[name]=@tableName AND
					[col].[name]=@columnName", new { schema = columnInfo.Schema, tableName = columnInfo.TableName, columnName = columnInfo.ColumnName });
			if (result != null)
			{
				constraintName = result.ConstraintName;
				clustered = result.IsClustered;
				return true;
			}

			return false;
		}

		public override string CreateSchemaStatement(string name)
		{
			return $"CREATE SCHEMA [{name}]";
		}

		public override string ApplyPaging(string sql, int pageNumber, int rowsPerPage)
		{
			string result = sql;

			result += $"\r\nOFFSET {rowsPerPage * (pageNumber - 1)} ROWS\r\nFETCH NEXT {rowsPerPage} ROWS ONLY";

			return result;
		}

		public override string GetScriptFromSaveException(SaveException exception)
		{
			throw new NotImplementedException();
		}

		public override string CreateEnumTableStatement(Type enumType)
		{
			EnumTableAttribute attr =
				enumType.GetAttribute<EnumTableAttribute>() ??
				Nullable.GetUnderlyingType(enumType).GetAttribute<EnumTableAttribute>() ??
				throw new Exception($"Enum type {enumType.Name} is missing an [EnumTable] attribute");

			return $"CREATE TABLE {ApplyDelimiter(attr.FullTableName())} (\r\n\t[Name] nvarchar(50) NOT NULL,\r\n\t[Value] int NOT NULL PRIMARY KEY\r\n)";
		}

		public override string InsertEnumValueStatement(string tableName, string name, int value)
		{
			return $"INSERT INTO {ApplyDelimiter(tableName)} ([Name], [Value]) VALUES ('{name}', {value})";
		}

		public override string CheckEnumValueExistsStatement(string tableName)
		{
			return $"{ApplyDelimiter(tableName)} WHERE [Name]=@name";
		}

		public override IEnumerable<TableInfo> GetTables(IDbConnection connection)
		{
			return connection.Query<TableInfo>("SELECT [Name], SCHEMA_NAME([schema_id]) AS [Schema] FROM sys.tables WHERE [type_desc]='USER_TABLE'");
		}

		public override IEnumerable<ColumnInfo> GetColumns(IDbConnection connection)
		{
			throw new NotImplementedException();
		}

		public override bool ForeignKeyExists(IDbConnection connection, PropertyInfo propertyInfo)
		{
			string fkName = propertyInfo.ForeignKeyName(this);
			return connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = fkName });
		}

		public override string ForeignKeyAddStatement(PropertyInfo propertyInfo)
		{
			return $"ALTER TABLE {GetTableName(propertyInfo.DeclaringType)} ADD " + ForeignKeyConstraintSyntax(propertyInfo);
		}

		public override string UniqueKeyStatement(PropertyInfo propertyInfo)
		{
			string columnName = propertyInfo.SqlColumnName();
			var tableInfo = GetTableInfoFromType(propertyInfo.DeclaringType);
			return $"ALTER TABLE {ApplyDelimiter(tableInfo.ToString())} ADD CONSTRAINT [U_{GetConstraintBaseName(propertyInfo.DeclaringType)}_{columnName}] UNIQUE ([{columnName}])";
		}
	}
}