using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge.Action
{
    public class AddColumn : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;
        private readonly DbObject _object;

        public AddColumn(PropertyInfo propertyInfo) : base(MergeObjectType.Column, MergeActionType.Create, $"Add column {propertyInfo.DeclaringType.Name}.{propertyInfo.Name}")
        {
            _propertyInfo = propertyInfo;
            _object = DbObject.FromType(_propertyInfo.DeclaringType);
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            yield return $"ALTER TABLE [{_object.Schema}].[{_object.Name}] ADD {_propertyInfo.SqlColumnSyntax()}";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            if (!connection.IsTableEmpty(_object.Schema, _object.Name) && !_propertyInfo.AllowSqlNull() && !_propertyInfo.HasAttribute<DefaultExpressionAttribute>())
            {
                yield return "Adding a non-nullable column to a table with data requires a [DefaultExpression] attribute on the column.";
            }
        }
    }
}
