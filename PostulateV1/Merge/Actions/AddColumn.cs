using Postulate.Orm.Abstract;
using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AddColumn : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;

        public AddColumn(SqlScriptGenerator scriptGen, PropertyInfo propertyInfo) : base(scriptGen, ObjectType.Column, ActionType.Create, $"Add column {propertyInfo.QualifiedName()}")
        {
            _propertyInfo = propertyInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}