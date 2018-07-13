using System;

namespace Postulate.Orm.Attributes
{
	public enum SupportedSyntax
	{
		SqlServer,
		MySql
	}

	/// <summary>
	/// Let's you specify the syntax to use when generating SQL from a model class with the ModelMerge command.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	public class DefaultSqlSyntaxAttribute : Attribute
	{
		private readonly SupportedSyntax _syntax;

		public DefaultSqlSyntaxAttribute(SupportedSyntax syntax)
		{
			_syntax = syntax;
		}

		public SupportedSyntax Syntax { get { return _syntax; } }
	}
}