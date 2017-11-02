using System.Reflection;

namespace Postulate.Orm.Merge.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static string DisplayName(this PropertyInfo propertyInfo)
        {
            return $"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}";
        }
    }
}