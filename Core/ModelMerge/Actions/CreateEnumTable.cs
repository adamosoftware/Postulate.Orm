using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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
                bool valueExists = false;
                if (connection.TableExists(_attribute.Schema, _attribute.TableName))
                {
                    valueExists = connection.Exists(Syntax.CheckEnumValueExistsStatement(tableName), new { name = name });
                }

				if (!valueExists)
				{
					yield return Syntax.InsertEnumValueStatement(tableName, name, (int)values.GetValue(index));
				}				

                index++;
            }

        }
    }
}