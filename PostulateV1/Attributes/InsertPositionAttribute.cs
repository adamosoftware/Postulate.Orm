using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	/// <summary>
	/// Indicates where in the target table inherited columns are added
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
	public class InsertPositionAttribute : Attribute
	{
		private readonly Position _insertPosition;

		public InsertPositionAttribute(Position insertPosition)
		{
			_insertPosition = insertPosition;
		}

		public Position Position { get { return _insertPosition; } }
	}
}
