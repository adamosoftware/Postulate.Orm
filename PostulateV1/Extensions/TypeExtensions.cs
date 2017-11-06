using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge.Action;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Extensions
{
    public static class TypeExtensions
    {
        public static string IdentityColumnName(this Type type)
        {
            string result = Record<int>.IdentityColumnName;

            IdentityColumnAttribute attr;
            if (type.HasAttribute(out attr)) result = attr.ColumnName;

            return result;
        }

        public static IEnumerable<PropertyInfo> GetPrimaryKeyProperties(this Type type)
        {
            var markedProperties = type
                .GetProperties().Where(pi => pi.HasAttribute<PrimaryKeyAttribute>())
                .OrderBy(pi => pi.SqlColumnName());

            if (markedProperties.Any())
            {
                foreach (var prop in markedProperties) yield return prop;
            }
            else
            {
                // if you make it here, it means there are no marked PK columns, so just return Id
                yield return type.GetProperty(Record<int>.IdentityColumnName);
            }
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

        public static bool HasClusteredPrimaryKey(this Type type)
        {
            return
                type.HasAttribute<ClusterAttribute>(attr => attr.Option == ClusterOption.PrimaryKey) ||
                !type.HasAttribute<ClusterAttribute>();
        }

        public static bool IsSupportedType(this Type type, SqlScriptGenerator scripGen)
        {
            return
                scripGen.SupportedTypes().ContainsKey(type) ||
                (type.IsEnum && type.GetEnumUnderlyingType().Equals(typeof(int))) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSupportedType(type.GetGenericArguments()[0], scripGen));
        }

        public static IEnumerable<PropertyInfo> GetModelPropertyInfo(this Type type, SqlScriptGenerator scriptGen)
        {
            return type.GetProperties().Where(pi => !pi.HasAttribute<NotMappedAttribute>() && scriptGen.IsSupportedType(pi.PropertyType));
        }

        public static IEnumerable<ColumnInfo> GetModelColumnInfo(this Type type, SqlScriptGenerator scriptGen)
        {
            return GetModelPropertyInfo(type, scriptGen).Select(pi => new ColumnInfo(pi));
        }

        public static IEnumerable<PropertyInfo> GetForeignKeys(this Type type)
        {
            return type.GetProperties().Where(pi => pi.IsForeignKey());
        }

    }
}