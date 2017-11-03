using Postulate.Orm.Merge.Extensions;
using Postulate.Orm.Merge.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AddForeignKey : Action2
    {
        private readonly PropertyInfo _propertyInfo;

        public AddForeignKey(PropertyInfo propertyInfo) : base(ObjectType.ForeignKey, ActionType.Create, propertyInfo.QualifiedName())
        {
            _propertyInfo = propertyInfo;
        }
    }
}