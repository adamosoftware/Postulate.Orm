using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Merge.Action;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool HasColumnAccess(this ICustomAttributeProvider provider, Access access)
        {
            PropertyInfo property = provider as PropertyInfo;
            if (property != null)
            {
                Type t = property.ReflectedType;
                ColumnAccessAttribute col;
                if (t.HasAttribute(out col, attr => attr.ColumnName.Equals(property.SqlColumnName())))
                {
                    return col.Access == access;
                }
            }

            return !provider.HasAttribute<ColumnAccessAttribute>() || provider.HasAttribute<ColumnAccessAttribute>(attr => attr.Access == access);
        }

        public static bool IsForSaveAction(this ICustomAttributeProvider provider, SaveAction action)
        {
            Dictionary<SaveAction, Access> map = new Dictionary<SaveAction, Access>()
            {
                { SaveAction.Insert, Access.InsertOnly },
                { SaveAction.Update, Access.UpdateOnly }
            };
            return HasColumnAccess(provider, map[action]);
        }

        public static bool IsSupportedType(this PropertyInfo propertyInfo)
        {
            return TypeExtensions.IsSupportedType(propertyInfo.PropertyType);
        }

        public static bool IsFKEnclosedBy(this PropertyInfo propertyInfo, IEnumerable<CreateTable> createTables)
        {
            return (createTables.Any(ct => ct.ContainsProperty(propertyInfo)) && createTables.Any(ct => ct.IsReferencedBy(propertyInfo)));
        }
    }
}