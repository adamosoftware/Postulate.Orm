using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge.Action
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
            DbObject obj = DbObject.FromType(_propertyInfo.DeclaringType);

            yield return $"ALTER TABLE [{obj.Schema}].[{obj.Name}] ADD {_propertyInfo.SqlColumnSyntax()}";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }
    }
}
