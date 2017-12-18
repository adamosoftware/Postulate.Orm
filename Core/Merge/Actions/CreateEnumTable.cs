using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge.Actions
{
    public class CreateEnumTable : MergeAction
    {
        private readonly Type _enumType;
        private readonly EnumTableAttribute _attribute;

        public CreateEnumTable(SqlSyntax syntax, Type enumType) : base(syntax, ObjectType.Table, ActionType.Create, $"Enum table {enumType.Name}")
        {
            _enumType = enumType;
            _attribute = enumType.GetAttribute<EnumTableAttribute>() ?? throw new Exception($"Enum type {enumType.Name} is missing an [EnumTable] attribute");
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {            
            if (!connection.TableExists(_attribute.Schema, _attribute.TableName))
            {
                yield return Syntax.CreateEnumTableStatement(_enumType);
            }

            var values = Enum.GetValues(_enumType).OfType<int>().ToArray();
            int index = 0;
            string tableName = _attribute.FullTableName();

            foreach (var name in Enum.GetNames(_enumType))
            {
                bool valueExists = false;
                if (connection.TableExists(_attribute.Schema, _attribute.TableName))
                {
                    valueExists = connection.Exists(Syntax.CheckEnumValueExistsStatement(tableName), new { name = name });
                }
                
                switch (_attribute.KeyType)
                {
                    case EnumTableKeyType.DefinedValues:
                        if (!valueExists)
                        {
                            yield return Syntax.InsertEnumValueStatement(tableName, name, values[index]);
                        }                        
                        break;

                    case EnumTableKeyType.GeneratedValues:
                        if (!valueExists)
                        {
                            yield return Syntax.InsertEnumValueStatement(tableName, name);
                        }                        
                        break;
                }

                index++;
            }

        }
    }
}