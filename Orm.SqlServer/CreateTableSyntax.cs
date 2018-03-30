using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.SqlServer
{
	public partial class SqlServerSyntax : SqlSyntax
	{
		public override string TableCreateStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns, bool withForeignKeys = false)
		{
			return $"CREATE TABLE {GetTableName(type)} (\r\n\t" +
				string.Join(",\r\n\t", CreateTableMembers(type, addedColumns, modifiedColumns, deletedColumns, withForeignKeys)) +
			"\r\n)";
		}

		public override string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns, bool withForeignKeys = false)
		{
			List<string> results = new List<string>();

			ClusterAttribute clusterAttribute = GetClusterAttribute(type);

			results.AddRange(CreateTableColumns(type, addedColumns, modifiedColumns, deletedColumns));

			results.Add(CreateTablePrimaryKey(type, clusterAttribute));

			results.AddRange(CreateTableUniqueConstraints(type, clusterAttribute));

			if (withForeignKeys)
			{
				var foreignKeys = type.GetForeignKeys();
				results.AddRange(foreignKeys.Select(pi => ForeignKeyConstraintSyntax(pi).RemoveAll("\r\n", "\t")));
			}

			return results.ToArray();
		}

		private ClusterAttribute GetClusterAttribute(Type type)
		{
			return type.GetCustomAttribute<ClusterAttribute>() ?? new ClusterAttribute(ClusterOption.PrimaryKey);
		}

		private IEnumerable<string> CreateTableColumns(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
		{
			List<string> results = new List<string>();

			Position identityPos = Position.StartOfTable;
			var ip = type.GetCustomAttribute<IdentityPositionAttribute>();
			if (ip == null) ip = type.BaseType.GetCustomAttribute<IdentityPositionAttribute>();
			if (ip != null) identityPos = ip.Position;

			if (identityPos == Position.StartOfTable) results.Add(IdentityColumnSql(type));

			results.AddRange(ColumnProperties(type).Select(pi =>
			{
				string result = GetColumnSyntax(pi);
				if (addedColumns?.Contains(pi.SqlColumnName()) ?? false) result += " /* added */";
				if (modifiedColumns?.Contains(pi.SqlColumnName()) ?? false) result += " /* modified */";
				return result;
			}));

			if (identityPos == Position.EndOfTable) results.Add(IdentityColumnSql(type));

			return results;
		}

		private string CreateTablePrimaryKey(Type type, ClusterAttribute clusterAttribute)
		{
			return $"CONSTRAINT [PK_{GetConstraintBaseName(type)}] PRIMARY KEY {clusterAttribute.Syntax(ClusterOption.PrimaryKey)}({string.Join(", ", PrimaryKeyColumns(type).Select(col => $"[{col}]"))})";
		}

		private IEnumerable<string> CreateTableUniqueConstraints(Type type, ClusterAttribute clusterAttribute)
		{
			List<string> results = new List<string>();

			if (PrimaryKeyColumns(type, markedOnly: true).Any())
			{
				results.Add($"CONSTRAINT [U_{GetConstraintBaseName(type)}_Id] UNIQUE {clusterAttribute.Syntax(ClusterOption.Identity)}([Id])");
			}

			results.AddRange(type.GetProperties().Where(pi => pi.HasAttribute<UniqueKeyAttribute>()).Select(pi =>
			{
				UniqueKeyAttribute attr = pi.GetCustomAttribute<UniqueKeyAttribute>();
				return $"CONSTRAINT [U_{GetConstraintBaseName(type)}_{pi.SqlColumnName()}] UNIQUE {attr.GetClusteredSyntax()}([{pi.SqlColumnName()}])";
			}));

			results.AddRange(type.GetCustomAttributes<UniqueKeyAttribute>().Select((u, i) =>
			{
				string constrainName = (string.IsNullOrEmpty(u.ConstraintName)) ? $"U_{GetConstraintBaseName(type)}_{i}" : u.ConstraintName;
				return $"CONSTRAINT [{constrainName}] UNIQUE {u.GetClusteredSyntax()}({string.Join(", ", u.ColumnNames.Select(col => $"[{col}]"))})";
			}));

			return results;
		}

		public override string TableCreateStatement(IDbConnection connection, TableInfo tableInfo)
		{
			throw new NotImplementedException();
		}
	}
}