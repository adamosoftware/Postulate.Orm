using System;

namespace Postulate.Orm.Attributes
{
    public enum Access
	{		
		InsertOnly,		
		UpdateOnly,
		ReadOnly
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
	public class ColumnAccessAttribute : Attribute
	{
		private readonly string _columnName; // used only in class mode
		private readonly Access _access;

		/// <summary>
		/// Describes how a column can be inserted or updated
		/// </summary>
		/// <param name="access"></param>
		public ColumnAccessAttribute(Access access)
		{
			_access = access;
		}

		/// <summary>
		/// Used with convention classes to describe column access regardless of which table the column appears in
		/// </summary>
		public ColumnAccessAttribute(string columnName, Access access)
		{
			_columnName = columnName;
			_access = access;
		}

		public Access Access { get { return _access; } }

		public string ColumnName { get { return _columnName; } }
	}
}
