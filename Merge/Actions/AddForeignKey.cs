using Postulate.Orm.Merge.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AddForeignKey : Action2
    {
        private readonly PropertyInfo _propertyInfo;

        public AddForeignKey(PropertyInfo propertyInfo) : base(Enum.ObjectType.ForeignKey, Enum.ActionType.Create, propertyInfo.QualifiedName())
        {
            _propertyInfo = propertyInfo;
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}