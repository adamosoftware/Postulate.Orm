using Postulate.Orm.Merge.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class DropColumn : Action2
    {
        public DropColumn(PropertyInfo propertyInfo) : base(Enum.ObjectType.Column, Enum.ActionType.Drop, $"Drop column {propertyInfo.QualifiedName()}", null)
        {
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}