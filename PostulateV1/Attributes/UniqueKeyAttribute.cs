using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
	public class UniqueKeyAttribute : Attribute
	{
		private readonly string[] _columnNames;
		private readonly string _constraintName;

		/// <summary>
		/// Denotes a unique constraint on a single property
		/// </summary>
		public UniqueKeyAttribute()
		{
		}

		/// <summary>
		/// At the class level, describes a unique constraint with a set of columns
		/// </summary>		
		public UniqueKeyAttribute(params string[] columnNames)
		{
			_columnNames = columnNames;
		}

		/// <summary>
		/// At the class level, describes a unique constraint with a given name and set of columns
		/// </summary>
		public UniqueKeyAttribute(string constraintName, string[] columnNames)
		{
			_columnNames = columnNames;
			_constraintName = constraintName;
		}

		public string[] ColumnNames { get { return _columnNames; } }

		public string ConstraintName { get { return _constraintName; } }
	}
}
