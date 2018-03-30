using System;

namespace Postulate.Orm.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
	public class RenameFromAttribute : Attribute
	{
		private readonly string _oldName;

		public RenameFromAttribute(string oldName)
		{
			_oldName = oldName;
		}

		public string OldName { get { return _oldName; } }
	}
}