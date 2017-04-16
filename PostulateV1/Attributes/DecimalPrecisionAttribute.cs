using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class DecimalPrecisionAttribute : Attribute
	{
		private byte _scale;
		private byte _precision;

		public DecimalPrecisionAttribute(byte precision, byte scale)
		{
			if (_precision < _scale) throw new ArgumentException("Precision must be equal or greater than scale.");

			_precision = precision;
			_scale = scale;			
		}

		public byte Scale { get { return _scale; } }
		public byte Precision { get { return _precision; } }
	}
}
