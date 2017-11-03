using Postulate.Orm.Merge.Enums;
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

        public AddColumn(PropertyInfo propertyInfo) : base(ObjectType.Column, ActionType.Create, $"Add column {propertyInfo.QualifiedName()}")
        {
            _propertyInfo = propertyInfo;
        }
    }
}