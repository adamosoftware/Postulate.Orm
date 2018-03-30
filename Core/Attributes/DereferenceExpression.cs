using System;

namespace Postulate.Orm.Attributes
{
	/// <summary>
	/// Indicates what to select from a model class when dereferencing a foreign key value when changes are analyzed by SqlDb.GetChanges
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class DereferenceExpression : Attribute
	{
		private readonly string _expr;

		public DereferenceExpression(string expression)
		{
			_expr = expression;
		}

		public string Expression { get { return _expr; } }
	}
}