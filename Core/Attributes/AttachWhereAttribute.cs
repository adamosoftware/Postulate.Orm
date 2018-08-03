using System;

namespace Postulate.Orm.Attributes
{
	/// <summary>
	/// Use this on <see cref="Query{TResult}"/> properties where you want to include criteria
	/// from the nested object's properties into the main query
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class AttachWhereAttribute : Attribute
	{
	}
}