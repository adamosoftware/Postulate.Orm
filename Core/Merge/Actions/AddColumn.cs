using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AddColumn : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;
        private readonly TableInfo _tableInfo;
        private readonly Type _modelType;

        public AddColumn(SqlSyntax scriptGen, PropertyInfo propertyInfo) : base(scriptGen, ObjectType.Column, ActionType.Create, $"{propertyInfo.QualifiedName()}")
        {
            _propertyInfo = propertyInfo;
            _modelType = propertyInfo.ReflectedType;
            _tableInfo = Syntax.GetTableInfoFromType(_modelType);
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            var modelType = _propertyInfo.ReflectedType;

            if (
                Syntax.TableExists(connection, modelType) &&
                !Syntax.IsTableEmpty(connection, modelType) &&
                !_propertyInfo.AllowSqlNull() &&
                !_propertyInfo.HasAttribute<DefaultExpressionAttribute>())
            {
                yield return "Adding a non-nullable column to a table with data requires a [DefaultExpression] attribute on the column.";
            }
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            DefaultExpressionAttribute def = _propertyInfo.GetAttribute<DefaultExpressionAttribute>();
            if (def?.IsConstant ?? true)
            {
                yield return Syntax.ColumnAddStatement(_tableInfo, _propertyInfo);                
            }
            else
            {
                yield return Syntax.ColumnAddStatement(_tableInfo, _propertyInfo, forceNull: true);
                yield return Syntax.UpdateColumnWithExpressionStatement(_tableInfo, _propertyInfo, def.Expression);
                yield return Syntax.ColumnAlterStatement(_tableInfo, _propertyInfo);
            }
        }
    }
}