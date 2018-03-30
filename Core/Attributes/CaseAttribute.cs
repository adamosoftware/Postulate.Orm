using System;

namespace Postulate.Orm.Attributes
{
	/// <summary>
	/// Used with classes based on <see cref="Abstract.Query{TResult}"/> to allow you to map specific criteria expressions with specific property values.
	/// Cannot be used with <see cref="WhereAttribute"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class CaseAttribute : Attribute
	{
		private readonly object _value;
		private readonly string _expression;

		public CaseAttribute(object value, string expression)
		{
			_value = value;
			_expression = expression;
		}

		public object Value { get { return _value; } }

		public string Expression { get { return _expression; } }
	}
}