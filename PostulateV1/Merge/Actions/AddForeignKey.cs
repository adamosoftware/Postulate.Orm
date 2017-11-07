using Postulate.Orm.Abstract;
using Postulate.Orm.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AddForeignKey : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;

        public AddForeignKey(SqlSyntax scriptGen, PropertyInfo propertyInfo) : base(scriptGen, ObjectType.ForeignKey, ActionType.Create, $"Add foreign key {propertyInfo.QualifiedName()}")
        {
            _propertyInfo = propertyInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}