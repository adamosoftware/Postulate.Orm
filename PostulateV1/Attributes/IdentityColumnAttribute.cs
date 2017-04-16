using System;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class IdentityColumnAttribute : Attribute
	{
		private readonly string _columnName;

		public IdentityColumnAttribute(string columnName)
		{
			_columnName = columnName;
		}

		public string ColumnName { get { return _columnName; } }
	}
}
