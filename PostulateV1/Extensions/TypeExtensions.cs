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
    }
}
