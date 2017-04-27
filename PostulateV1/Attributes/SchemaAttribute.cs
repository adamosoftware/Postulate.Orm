using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class SchemaAttribute : Attribute
	{
		private readonly string _schema;

		public SchemaAttribute(string schema)
		{
			_schema = schema;
		}

		public string Schema { get { return _schema; } }
	}
}
