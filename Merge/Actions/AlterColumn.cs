using Postulate.Orm.Merge.Extensions;
using Postulate.Orm.Merge.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AlterColumn : Action2
    {
        public AlterColumn(PropertyInfo propertyInfo) : base(ObjectType.Column, ActionType.Alter, $"Altering {propertyInfo.QualifiedName()}")
        {
        }
    }
}