using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge.Models;
using ReflectionHelper;
using System;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge.Extensions
{
    public static class PropertyInfoExtensions
    {
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