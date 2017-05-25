using Postulate.Orm.Attributes;
using Postulate.Orm.Merge;
using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using ReflectionHelper;

namespace Postulate.Orm.Extensions
{
    public static class PropertyInfoSqlExtensions
    {
        public static string SqlColumnSyntax(this PropertyInfo propertyInfo)
        {
            string result = null;

            CalculatedAttribute calc;
            if (propertyInfo.HasAttribute(out calc))
            {
                result = $"[{propertyInfo.SqlColumnName()}] AS {calc.Expression}";
            }
            else
            {
                result = $"[{propertyInfo.SqlColumnName()}] {propertyInfo.SqlColumnType()}{propertyInfo.SqlDefaultExpression(forCreateTable: true)}";
            }

            return result;
        }

        public static string SqlColumnName(this PropertyInfo propertyInfo)
        {
            string result = propertyInfo.Name;

            ColumnAttribute attr;
            if (propertyInfo.HasAttribute(out attr, a => !string.IsNullOrEmpty(a.Name))) result = attr.Name;

            return result;
        }

        public static string SqlDataType(this PropertyInfo propertyInfo)
        {
            string result = null;

            ColumnAttribute colAttr;
            if (propertyInfo.HasAttribute(out colAttr))
            {
                return colAttr.TypeName;
            }
            else
            {
                string length = "max";
                var maxLenAttr = propertyInfo.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLenAttr != null) length = maxLenAttr.Length.ToString();

                byte precision = 5, scale = 2; // some aribtrary defaults
                var dec = propertyInfo.GetCustomAttribute<DecimalPrecisionAttribute>();
                if (dec != null)
                {
                    precision = dec.Precision;
                    scale = dec.Scale;
                }

                var typeMap = CreateTable.SupportedTypes(length, precision, scale);

                Type t = propertyInfo.PropertyType;
                if (t.IsGenericType) t = t.GenericTypeArguments[0];
                if (t.IsEnum) t = t.GetEnumUnderlyingType();

                if (!typeMap.ContainsKey(t)) throw new KeyNotFoundException($"Type name {t.Name} not supported.");

                result = typeMap[t];
            }

            return result;
        }

        public static string SqlColumnType(this PropertyInfo propertyInfo)
        {
            string nullable = ((AllowSqlNull(propertyInfo)) ? "NULL" : "NOT NULL");

            string result = SqlDataType(propertyInfo);

            CollateAttribute collate;
            string collation = string.Empty;
            if (propertyInfo.HasAttribute(out collate)) collation = $"COLLATE {collate.Collation} ";

            return $"{result} {collation}{nullable}";
        }

        public static string SqlDefaultExpression(this PropertyInfo propertyInfo, bool forCreateTable = false)
        {
            string template = (forCreateTable) ? " DEFAULT ({0})" : "{0}";

            DefaultExpressionAttribute def;
            if (propertyInfo.DeclaringType.HasAttribute(out def) && propertyInfo.Name.Equals(def.ColumnName)) return string.Format(template, Quote(propertyInfo, def.Expression));
            if (propertyInfo.HasAttribute(out def)) return string.Format(template, Quote(propertyInfo, def.Expression));

            // if the expression is part of a CREATE TABLE statement, it's not necessary to go any further
            if (forCreateTable) return null;

            if (propertyInfo.AllowSqlNull()) return "NULL";

            throw new Exception($"{propertyInfo.DeclaringType.Name}.{propertyInfo.Name} property does not have a [DefaultExpression] attribute.");
        }

        private static string Quote(PropertyInfo propertyInfo, string expression)
        {
            string result = expression;

            var quotedTypes = new Type[] { typeof(string), typeof(DateTime) };
            if (quotedTypes.Any(t => t.Equals(propertyInfo.PropertyType)))
            {
                if (result.Contains("'") && !result.StartsWith("'") && !result.EndsWith("'")) result = result.Replace("'", "''");
                if (!result.StartsWith("'")) result = "'" + result;
                if (!result.EndsWith("'")) result = result + "'";
            }

            return result;
        }

        public static Attributes.ForeignKeyAttribute GetForeignKeyAttribute(this PropertyInfo propertyInfo)
        {
            Attributes.ForeignKeyAttribute fk;
            if (propertyInfo.HasAttribute(out fk)) return fk;

            fk = propertyInfo.DeclaringType.GetCustomAttributes<Attributes.ForeignKeyAttribute>()
                .SingleOrDefault(attr => attr.ColumnName.Equals(propertyInfo.Name));
            if (fk != null) return fk;

            throw new ArgumentException($"The property {propertyInfo.Name} does not have a [ForeignKey] attribute.");
        }

        public static Type GetForeignKeyType(this PropertyInfo propertyInfo)
        {
            var fkAttr = GetForeignKeyAttribute(propertyInfo);
            return (fkAttr != null) ? fkAttr.PrimaryTableType : null;
        }

        public static string ForeignKeyName(this PropertyInfo propertyInfo)
        {
            var fk = GetForeignKeyAttribute(propertyInfo);
            return $"FK_{DbObject.ConstraintName(propertyInfo.DeclaringType)}_{propertyInfo.SqlColumnName()}";
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

        public static DbObject GetDbObject(this PropertyInfo propertyInfo, IDbConnection connection = null)
        {
            return DbObject.FromType(propertyInfo.ReflectedType, connection);
        }

        public static IEnumerable<string> GetPrimaryKeyValidationErrors(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Primary key column [{propertyInfo.Name}] may not use MAX size.";
            if (propertyInfo.PropertyType.IsNullableGeneric()) yield return $"Primary key column [{propertyInfo.Name}] may not be nullable.";
        }        

        public static string IndexName(this PropertyInfo propertyInfo)
        {
            return $"IX_{DbObject.ConstraintName(propertyInfo.DeclaringType)}_{propertyInfo.SqlColumnName()}";
        }
    }
}
