using System;

namespace Postulate.Orm.Attributes
{
	[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
	public class EnumTableAttribute : Attribute
	{
		private readonly string _schema;
		private readonly string _tableName;

		public EnumTableAttribute(string tableName, string schema = null)
		{
			_schema = schema;
			_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
		}

		public string Schema { get { return _schema; } }

		public string TableName { get { return _tableName; } }

		public string FullTableName()
		{
			return (!string.IsNullOrEmpty(Schema)) ? $"{Schema}.{TableName}" : TableName;
		}
	}
}