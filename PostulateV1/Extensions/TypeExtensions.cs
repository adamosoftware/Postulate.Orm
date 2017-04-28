using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Merge.Diff;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postulate.Extensions
{
    public static class TypeExtensions
    {
        public static string IdentityColumnName(this Type type)
        {
            string result = Record<int>.IdColumnName;

            IdentityColumnAttribute attr;
            if (type.HasAttribute(out attr)) result = attr.ColumnName;

            return result;
        }

        public static bool IsNullable(this Type type)
        {
            return IsNullableGeneric(type) || type.Equals(typeof(string)) || type.Equals(typeof(byte[]));
        }

        public static bool IsNullableGeneric(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        // adapted from http://stackoverflow.com/questions/17058697/determining-if-type-is-a-subclass-of-a-generic-type
        public static bool IsDerivedFromGeneric(this Type type, Type genericType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(genericType)) return true;
            if (type.BaseType != null) return IsDerivedFromGeneric(type.BaseType, genericType);
            return false;
        }

        public static string GetSchema(this Type type)
        {
            string schema, name;
            CreateTable.ParseNameAndSchema(type, out schema, out name);
            return schema;
        }

        public static string GetTableName(this Type type)
        {
            string schema, name;
            CreateTable.ParseNameAndSchema(type, out schema, out name);
            return name;
        }
    }
}
