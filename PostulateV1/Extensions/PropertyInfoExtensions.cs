using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Models;
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

        public static bool IsSupportedType(this PropertyInfo propertyInfo, SqlSyntax syntax)
        {
            return syntax.SupportedTypes().ContainsKey(propertyInfo.PropertyType);
        }

        public static string QualifiedName(this PropertyInfo propertyInfo)
        {
            return $"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}";
        }

        public static bool IsForeignKey(this PropertyInfo propertyInfo)
        {
            var fk = GetForeignKeyInfo(propertyInfo);
            return (fk != null);
        }

        public static ForeignKeyAttribute GetForeignKeyInfo(this PropertyInfo propertyInfo)
        {
            ForeignKeyAttribute attr;
            if (propertyInfo.HasAttribute(out attr)) return attr;

            Type[] types = { propertyInfo.DeclaringType, propertyInfo.ReflectedType };
            var fkType = types.FirstOrDefault(t => t.HasAttribute(out attr, a => a.ColumnName.Equals(propertyInfo.SqlColumnName())));
            return attr;
        }

        public static ColumnInfo ToColumnInfo(this PropertyInfo propertyInfo)
        {
            return new ColumnInfo(propertyInfo);
        }
    }
}