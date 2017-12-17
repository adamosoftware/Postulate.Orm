using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Actions
{
    public class EnumTable : MergeAction
    {
        private readonly Type _enumType;
        private readonly EnumTableAttribute _attribute;

        public EnumTable(SqlSyntax syntax, Type enumType) : base(syntax, ObjectType.Table, ActionType.Create, $"Enum table {enumType.Name}")
        {
            _enumType = enumType;
            _attribute = enumType.GetAttribute<EnumTableAttribute>() ?? throw new Exception($"Enum type {enumType.Name} is missing an [EnumTable] attribute");
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}