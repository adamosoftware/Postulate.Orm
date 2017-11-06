using Postulate.Orm.Abstract;
using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AlterColumn : MergeAction
    {
        public AlterColumn(SqlScriptGenerator scriptGen, PropertyInfo propertyInfo) : base(scriptGen, ObjectType.Column, ActionType.Alter, $"Altering {propertyInfo.QualifiedName()}")
        {
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}