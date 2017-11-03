using Postulate.Orm.Merge.Extensions;
using Postulate.Orm.Merge.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class DropColumn : Action2
    {
        public DropColumn(PropertyInfo propertyInfo) : base(ObjectType.Column, ActionType.Drop, $"Drop column {propertyInfo.QualifiedName()}")
        {
        }
    }
}