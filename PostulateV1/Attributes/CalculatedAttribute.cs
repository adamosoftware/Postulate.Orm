using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class CalculatedAttribute : Attribute
	{
		private readonly string _expression;
		private readonly bool _persist;

		public CalculatedAttribute(string expression, bool isPersistent = false)
		{
			_expression = expression;
			_persist = isPersistent;
		}

		public string Expression { get { return _expression; } }

		public bool IsPersistent { get { return _persist; } }
	}
}
