using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Models;
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
			return syntax.IsSupportedType(propertyInfo.PropertyType);
		}

		public static string QualifiedName(this PropertyInfo propertyInfo)
		{
			return $"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}";
		}

		public static ColumnInfo ToColumnInfo(this PropertyInfo propertyInfo, SqlSyntax syntax)
		{
			return ColumnInfo.FromPropertyInfo(propertyInfo, syntax);
		}

		public static bool HasAttribute<TAttribute>(this ICustomAttributeProvider provider, out TAttribute attribute, Func<TAttribute, bool> predicate = null) where TAttribute : Attribute
		{
			attribute = GetAttribute<TAttribute>(provider);

			if (attribute != null)
			{
				return predicate?.Invoke(attribute) ?? true;
			}
			else
			{
				return false;
			}
		}

		public static bool HasAttribute<TAttribute>(this ICustomAttributeProvider provider, Func<TAttribute, bool> predicate = null) where TAttribute : Attribute
		{
			TAttribute attr;
			return HasAttribute(provider, out attr, predicate);
		}

		public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider provider) where TAttribute : Attribute
		{
			var attrs = provider.GetCustomAttributes(typeof(TAttribute), true).OfType<TAttribute>();
			return (attrs?.Any() ?? false) ? attrs.FirstOrDefault() : null;
		}
	}
}