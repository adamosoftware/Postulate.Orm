using Postulate.Orm.Merge.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AddColumn : Action2
    {
        private readonly PropertyInfo _propertyInfo;

        public AddColumn(PropertyInfo propertyInfo) : base(Enum.ObjectType.Column, Enum.ActionType.Create, $"Add column {propertyInfo.QualifiedName()}")
        {
            _propertyInfo = propertyInfo;
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}