using Postulate.Orm.Merge.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AlterColumn : Action2
    {
        public AlterColumn(PropertyInfo propertyInfo) : base(Enum.ObjectType.Column, Enum.ActionType.Alter, $"Altering {propertyInfo.QualifiedName()}")
        {
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}