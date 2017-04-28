using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge.Diff
{
    public class AddColumn : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;

        public AddColumn(PropertyInfo propertyInfo) : base(MergeObjectType.Column, MergeActionType.Create, $"Add column {propertyInfo.DeclaringType.Name}.{propertyInfo.Name}")
        {
            _propertyInfo = propertyInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
