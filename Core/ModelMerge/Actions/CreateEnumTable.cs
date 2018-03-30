using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Postulate.Orm.ModelMerge.Actions
{
	public class CreateEnumTable : MergeAction
	{
		private readonly Type _enumType;
		private readonly EnumTableAttribute _attribute;

		public static string EnumTableName(Type enumType)
		{
			return (!enumType.IsNullableEnum()) ? enumType.Name : Nullable.GetUnderlyingType(enumType).Name;
		}

		public CreateEnumTable(SqlSyntax syntax, Type enumType) : base(syntax, ObjectType.Table, ActionType.Create, $"Enum table {EnumTableName(enumType)}")
		{
			_enumType = (!enumType.IsNullableEnum()) ? enumType : Nullable.GetUnderlyingType(enumType);
			_attribute =
				enumType.GetAttribute<EnumTableAttribute>() ??
				Nullable.GetUnderlyingType(enumType).GetAttribute<EnumTableAttribute>() ??
				throw new Exception($"Enum type {enumType.Name} is missing an [EnumTable] attribute");
		}

		public override IEnumerable<string> SqlCommands(IDbConnection connection)
		{
			if (!connection.TableExists(_attribute.Schema, _attribute.TableName))
			{
				yield return Syntax.CreateEnumTableStatement(_enumType);
			}

			var values = Enum.GetValues(_enumType);
			int index = 0;
			string tableName = _attribute.FullTableName();

			foreach (var name in Enum.GetNames(_enumType))
			{
				string formattedName = FormatEnumValueName(name);
				bool valueExists = false;
				if (connection.TableExists(_attribute.Schema, _attribute.TableName))
				{
					valueExists = connection.Exists(Syntax.CheckEnumValueExistsStatement(tableName), new { name = formattedName });
				}

				if (!valueExists)
				{
					yield return Syntax.InsertEnumValueStatement(tableName, formattedName, (int)values.GetValue(index));
				}

				index++;
			}
		}

		/// <summary>
		/// Inserts spaces between lower case and upper case letters in a name
		/// </summary>
		private string FormatEnumValueName(string name)
		{
			string result = name;

			while (true)
			{
				var match = Regex.Match(result, "[a-z][A-Z]");
				if (match == null) break;
				if (match.Index == 0) break;
				result = result.Substring(0, match.Index + 1) + " " + result.Substring(match.Index + 1);
			}

			return result;
		}

		/// <summary>
		/// Indicates whether there are any statements to run for this enumType.
		/// Since enum script can be incrementally added, it's possible to script an action with no commands, and we want to prevent that
		/// </summary>
		public static bool ShouldRun(IDbConnection connection, SqlSyntax syntax, Type enumType)
		{
			var action = new CreateEnumTable(syntax, enumType);
			return action.SqlCommands(connection).Any();
		}
	}
}