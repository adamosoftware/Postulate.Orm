using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.SqlServer
{
    public partial class SqlServerSyntax : SqlSyntax
    {
        public override string GetCreateTableStatement(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
        {
            return $"CREATE TABLE {GetTableName(type)} (\r\n\t" +
                string.Join(",\r\n\t", CreateTableMembers(type, addedColumns, modifiedColumns, deletedColumns)) +
            "\r\n)";
        }

        public override string[] CreateTableMembers(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
        {
            List<string> results = new List<string>();

            ClusterAttribute clusterAttribute = GetClusterAttribute(type);

            results.AddRange(CreateTableColumns(type, addedColumns, modifiedColumns, deletedColumns));

            results.Add(CreateTablePrimaryKey(type, clusterAttribute));

            results.AddRange(CreateTableUniqueConstraints(type, clusterAttribute));

            return results.ToArray();
        }

        private ClusterAttribute GetClusterAttribute(Type type)
        {
            return type.GetCustomAttribute<ClusterAttribute>() ?? new ClusterAttribute(ClusterOption.PrimaryKey);
        }        

        private IEnumerable<string> CreateTableColumns(Type type, IEnumerable<string> addedColumns, IEnumerable<string> modifiedColumns, IEnumerable<string> deletedColumns)
        {
            List<string> results = new List<string>();

            Position identityPos = Position.StartOfTable;
            var ip = type.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip == null) ip = type.BaseType.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip != null) identityPos = ip.Position;

            if (identityPos == Position.StartOfTable) results.Add(IdentityColumnSql(type));

            results.AddRange(ColumnProperties(type).Select(pi =>
            {
                string result = GetColumnSyntax(pi);
                if (addedColumns?.Contains(pi.SqlColumnName()) ?? false) result += " /* added */";
                if (modifiedColumns?.Contains(pi.SqlColumnName()) ?? false) result += " /* modified */";
                return result;
            }));

            if (identityPos == Position.EndOfTable) results.Add(IdentityColumnSql(type));

            return results;
        }

        public IEnumerable<PropertyInfo> ColumnProperties(Type type)
        {
            return type.GetProperties()
                .Where(p =>
                    p.CanWrite &&
                    !p.Name.ToLower().Equals(nameof(Record<int>.Id).ToLower()) &&
                    IsSupportedType(p.PropertyType) &&                    
                    !p.HasAttribute<NotMappedAttribute>());
        }

        private string IdentityColumnSql(Type type)
        {
            Type keyType = FindKeyType(type);

            return $"{ApplyDelimiter(type.IdentityColumnName())} {KeyTypeMap()[keyType]}";
        }

        private Type FindKeyType(Type modelType)
        {
            if (!modelType.IsDerivedFromGeneric(typeof(Record<>))) throw new ArgumentException("Model class must derive from Record<TKey>");

            Type checkType = modelType;
            while (!checkType.IsGenericType) checkType = checkType.BaseType;
            return checkType.GetGenericArguments()[0];
        }

        private string CreateTablePrimaryKey(Type type, ClusterAttribute clusterAttribute)
        {
            return $"CONSTRAINT [PK_{GetConstraintBaseName(type)}] PRIMARY KEY {clusterAttribute.Syntax(ClusterOption.PrimaryKey)}({string.Join(", ", PrimaryKeyColumns(type).Select(col => $"[{col}]"))})";
        }

        private static IEnumerable<PropertyInfo> PrimaryKeyProperties(Type type, bool markedOnly = false)
        {
            var pkProperties = type.GetProperties().Where(pi => pi.HasAttribute<PrimaryKeyAttribute>());
            if (pkProperties.Any() || markedOnly) return pkProperties;
            return new PropertyInfo[] { type.GetProperty(type.IdentityColumnName()) };
        }

        private static IEnumerable<string> PrimaryKeyColumns(Type type, bool markedOnly = false)
        {
            return PrimaryKeyProperties(type, markedOnly).Select(pi => pi.SqlColumnName());
        }

        private IEnumerable<string> CreateTableUniqueConstraints(Type type, ClusterAttribute clusterAttribute)
        {
            List<string> results = new List<string>();

            if (PrimaryKeyColumns(type, markedOnly: true).Any())
            {
                results.Add($"CONSTRAINT [U_{GetConstraintBaseName(type)}_Id] UNIQUE {clusterAttribute.Syntax(ClusterOption.Identity)}([Id])");
            }

            results.AddRange(type.GetProperties().Where(pi => pi.HasAttribute<UniqueKeyAttribute>()).Select(pi =>
            {
                UniqueKeyAttribute attr = pi.GetCustomAttribute<UniqueKeyAttribute>();
                return $"CONSTRAINT [U_{GetConstraintBaseName(type)}_{pi.SqlColumnName()}] UNIQUE {attr.GetClusteredSyntax()}([{pi.SqlColumnName()}])";
            }));

            results.AddRange(type.GetCustomAttributes<UniqueKeyAttribute>().Select((u, i) =>
            {
                string constrainName = (string.IsNullOrEmpty(u.ConstraintName)) ? $"U_{GetConstraintBaseName(type)}_{i}" : u.ConstraintName;
                return $"CONSTRAINT [{constrainName}] UNIQUE {u.GetClusteredSyntax()}({string.Join(", ", u.ColumnNames.Select(col => $"[{col}]"))})";
            }));

            return results;
        }

    }
}