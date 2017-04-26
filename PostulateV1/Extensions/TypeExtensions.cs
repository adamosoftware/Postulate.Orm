using Postulate.Abstract;
using Postulate.Attributes;
using System;

namespace Postulate.Extensions
{
    public static class TypeExtensions
    {
        public static string IdentityColumnName<TKey>(this Type type)
        {
            string result = SqlDb<TKey>.IdentityColumnName;

            IdentityColumnAttribute attr;
            if (type.HasAttribute(out attr)) result = attr.ColumnName;

            return result;
        }

        // adapted from http://stackoverflow.com/questions/17058697/determining-if-type-is-a-subclass-of-a-generic-type
        public static bool IsDerivedFromGeneric(this Type type, Type genericType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(genericType)) return true;
            if (type.BaseType != null) return IsDerivedFromGeneric(type.BaseType, genericType);
            return false;
        }
    }
}
