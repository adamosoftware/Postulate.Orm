using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using ReflectionHelper;
using System.Collections.Generic;
using System.Reflection;

namespace Postulate.Orm.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool HasColumnAccess(this ICustomAttributeProvider provider, Access access)
        {
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
    }
}