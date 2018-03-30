using System;

namespace Postulate.Orm.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class CollateAttribute : Attribute
	{
		private readonly string _collation;

		public CollateAttribute(string collation)
		{
			_collation = collation;
		}

		public string Collation { get { return _collation; } }
	}
}