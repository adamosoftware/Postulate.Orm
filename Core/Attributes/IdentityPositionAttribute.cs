using Postulate.Orm.Enums;
using System;

namespace Postulate.Orm.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class IdentityPositionAttribute : Attribute
	{
		private readonly Position _position;

		public IdentityPositionAttribute(Position position)
		{
			_position = position;
		}

		public Position Position { get { return _position; } }
	}
}