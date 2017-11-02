using Postulate.Orm.Extensions;
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
        public static IEnumerable<PropertyInfo> GetModelProperties(this Type type)
        {
            return type.GetProperties().Where(pi => !pi.HasAttribute<NotMappedAttribute>() && pi.IsSupportedType());
        }
    }
}