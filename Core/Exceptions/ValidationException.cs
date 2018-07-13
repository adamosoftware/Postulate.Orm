using System;

namespace Postulate.Orm.Exceptions
{
	public class ValidationException : Exception
	{
		public ValidationException(string message) : base(message)
		{
		}
	}
}