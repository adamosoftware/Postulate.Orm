using Postulate.Orm.Merge.Abstract;
using Postulate.Orm.Merge.Extensions;
using System.Reflection;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.MergeActions
{
    public class AddForeignKey : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;

        public AddForeignKey(SqlScriptGenerator scriptGen, PropertyInfo propertyInfo) : base(scriptGen, ObjectType.ForeignKey, ActionType.Create, propertyInfo.QualifiedName())
        {
            _propertyInfo = propertyInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}