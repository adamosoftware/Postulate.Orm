using System;

namespace Postulate.Orm.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class TrackChangesAttribute : Attribute
	{
		public TrackChangesAttribute()
		{
		}

		public string IgnoreProperties { get; set; }
	}
}
