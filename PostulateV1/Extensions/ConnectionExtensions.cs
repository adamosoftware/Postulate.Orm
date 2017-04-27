using System.Data;
using Dapper;

namespace Postulate.Extensions
{
	public static class ConnectionExtensions
	{
		public static bool Exists(this IDbConnection connection, string fromWhere, object parameters = null)
		{
			return ((connection.QueryFirstOrDefault<int?>($"SELECT 1 FROM {fromWhere}", parameters) ?? 0) == 1);
		}

		public static bool ForeignKeyExists(this IDbConnection connection, string name)
		{
			return connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = name });
		}

		public static bool ColumnExists(this IDbConnection connection, string schema, string tableName, string columnName)
		{
			return connection.Exists(
				@"[sys].[columns] [col] INNER JOIN [sys].[tables] [tbl] ON [col].[object_id]=[tbl].[object_id]
				WHERE SCHEMA_NAME([tbl].[schema_id])=@schema AND [tbl].[name]=@tableName AND [col].[name]=@columnName",
				new { schema = schema, tableName = tableName, columnName = columnName });
		}
	}
}
