using System;

namespace Postulate.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class PrimaryKeyAttribute : Attribute
	{
	}
}
