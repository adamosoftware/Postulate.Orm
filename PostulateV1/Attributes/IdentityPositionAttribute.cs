using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
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
