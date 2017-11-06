using Postulate.Orm.Merge.Abstract;
using Postulate.Orm.Merge.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge.Extensions
{
    public static class TypeExtensions
    {
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