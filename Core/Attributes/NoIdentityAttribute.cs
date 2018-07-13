using System;

namespace Postulate.Orm.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class NoIdentityAttribute : Attribute
	{
	}
}