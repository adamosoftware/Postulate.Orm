using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Attributes
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
	public class DefaultExpressionAttribute : Attribute
	{
		private readonly string _columnName;
		private readonly string _expression;
        private readonly bool _isConstant;

		public DefaultExpressionAttribute(string expression, bool isConstant = true)
		{
			_expression = expression;
            _isConstant = isConstant;
		}

		public DefaultExpressionAttribute(string columnName, string expression, bool isConstant = true)
		{
			_columnName = columnName;
			_expression = expression;
            _isConstant = isConstant;
		}

		public string Expression { get { return _expression; } }

		public string ColumnName { get { return _columnName; } }

        public bool IsConstant { get { return _isConstant; } }
	}
}
