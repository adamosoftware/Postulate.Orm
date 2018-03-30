using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Extensions
{
	public static class PropertyInfoSqlExtensions
	{
		public static string SqlColumnName(this PropertyInfo propertyInfo)
		{
			string result = propertyInfo.Name;

			ColumnAttribute attr;
			if (propertyInfo.HasAttribute(out attr, a => !string.IsNullOrEmpty(a.Name))) result = attr.Name;

			return result;
		}

		public static bool IsEnumForeignKey(this PropertyInfo propertyInfo, out EnumTableAttribute enumTableAttribute)
		{
			EnumTableAttribute enumTableAttribute1 = null;
			EnumTableAttribute enumTableAttribute2 = null;

			bool result =
				(propertyInfo.PropertyType.IsEnum || propertyInfo.PropertyType.IsNullableEnum()) &&
				(propertyInfo.PropertyType.HasAttribute(out enumTableAttribute1) || Nullable.GetUnderlyingType(propertyInfo.PropertyType).HasAttribute(out enumTableAttribute2));

			enumTableAttribute = enumTableAttribute1 ?? enumTableAttribute2;

			return result;
		}

		public static bool IsEnumForeignKey(this PropertyInfo propertyInfo)
		{
			EnumTableAttribute attr;
			return IsEnumForeignKey(propertyInfo, out attr);
		}

		public static bool IsForeignKey(this PropertyInfo propertyInfo)
		{
			try
			{
				var fk = GetForeignKeyAttribute(propertyInfo);
				if (fk != null) return true;
				if (IsEnumForeignKey(propertyInfo)) return true;
				return false;
			}
			catch
			{
				return false;
			}
		}

		public static Attributes.ForeignKeyAttribute GetForeignKeyAttribute(this PropertyInfo propertyInfo)
		{
			Attributes.ForeignKeyAttribute fk;
			if (propertyInfo.HasAttribute(out fk)) return fk;

			fk = propertyInfo.DeclaringType.GetCustomAttributes<Attributes.ForeignKeyAttribute>()
				.SingleOrDefault(attr => attr.ColumnName.Equals(propertyInfo.Name));

			return (fk != null) ? fk : null;
		}

		public static Type GetForeignKeyParentType(this PropertyInfo propertyInfo)
		{
			var fkAttr = GetForeignKeyAttribute(propertyInfo);
			return (fkAttr != null) ? fkAttr.PrimaryTableType : null;
		}

		public static string ForeignKeyName(this PropertyInfo propertyInfo, SqlSyntax syntax)
		{
			return $"FK_{syntax.GetConstraintBaseName(propertyInfo.ReflectedType)}_{propertyInfo.SqlColumnName()}";
		}

		public static bool AllowSqlNull(this PropertyInfo propertyInfo)
		{
			if (InPrimaryKey(propertyInfo)) return false;
			if (propertyInfo.HasAttribute<RequiredAttribute>()) return false;
			return propertyInfo.PropertyType.IsNullable();
		}

		public static bool InPrimaryKey(this PropertyInfo propertyInfo)
		{
			return propertyInfo.HasAttribute<PrimaryKeyAttribute>();
		}

		public static TableInfo GetTableInfo(this PropertyInfo propertyInfo, IDbConnection connection = null)
		{
			return TableInfo.FromModelType(propertyInfo.ReflectedType);
		}

		public static IEnumerable<string> GetPrimaryKeyValidationErrors(this PropertyInfo propertyInfo, SqlSyntax syntax)
		{
			if (syntax.SqlDataType(propertyInfo).ToLower().Contains("car(max)")) yield return $"Primary key column {propertyInfo.Name} may not use MAX size.";
			if (propertyInfo.PropertyType.IsNullableGeneric()) yield return $"Primary key column {propertyInfo.Name} may not be nullable.";
		}

		public static string IndexName(this PropertyInfo propertyInfo, SqlSyntax syntax)
		{
			return $"IX_{syntax.GetConstraintBaseName(propertyInfo.DeclaringType)}_{propertyInfo.SqlColumnName()}";
		}
	}
}