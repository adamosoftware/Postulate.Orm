using Postulate.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data;
using System.Reflection;

namespace Postulate.Validation
{
	public enum Patterns
	{
		Email
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class RegexAttribute : ValidationAttribute
	{
		private readonly string _pattern;

		public RegexAttribute(Patterns pattern, string message) : base(message)
		{
			var dictionary = new Dictionary<Patterns, string>()
			{
				{ Patterns.Email, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}\b" }
			};
			_pattern = dictionary[pattern];
		}

		public string Pattern { get { return _pattern; } }

		public override bool IsValid(PropertyInfo property, object value, IDbConnection connection = null)
		{
			if (value != null)
			{
				return Regex.IsMatch(value.ToString(), _pattern, RegexOptions.IgnoreCase);
			}
			return true;
		}
	}
}
