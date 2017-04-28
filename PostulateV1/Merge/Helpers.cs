using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Reflection;
using Postulate.Extensions;
using Postulate.Attributes;

namespace Postulate.Merge
{
    internal static class Helpers
    {
        internal static IEnumerable<ForeignKeyRef> GetReferencingForeignKeys(this IDbConnection connection, int objectID)
        {
            return connection.Query(
                @"SELECT [fk].[name] AS [ConstraintName], SCHEMA_NAME([tbl].[schema_id]) AS [ReferencingSchema], [tbl].[name] AS [ReferencingTable] 
				FROM [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [tbl] ON [fk].[parent_object_id]=[tbl].[object_id] 
				WHERE [referenced_object_id]=@objID", new { objID = objectID })
                .Select(fk => new ForeignKeyRef() { ConstraintName = fk.ConstraintName, ReferencingTable = new DbObject(fk.ReferencingSchema, fk.ReferencingTable) });
        }

        internal static IEnumerable<PropertyInfo> GetModelForeignKeys(this Type modelType)
        {
            foreach (var pi in modelType.GetProperties().Where(pi => pi.HasAttribute<ForeignKeyAttribute>()))
            {
                yield return pi;
            }

            foreach (var attr in modelType.GetCustomAttributes<ForeignKeyAttribute>()
                .Where(attr => HasColumnName(modelType, attr.ColumnName)))
            {
                PropertyInfo pi = modelType.GetProperty(attr.ColumnName);
                if (pi != null) yield return pi;
            }
        }

        internal static IEnumerable<ForeignKeyRef> GetModelReferencingForeignKeys(this Type modelType, IEnumerable<Type> allTypes)
        {
            return allTypes.SelectMany(t => GetModelForeignKeys(t).Where(pi =>
            {
                ForeignKeyAttribute fk = pi.GetForeignKeyAttribute();
                return (fk.PrimaryTableType.Equals(modelType));
            }).Select(pi =>
                new ForeignKeyRef() { ConstraintName = pi.ForeignKeyName(), ReferencingTable = DbObject.FromType(pi.DeclaringType) }
            ));
        }

        internal static IEnumerable<string> GetFKDropStatements(this IDbConnection connection, int objectId)
        {
            var foreignKeys = connection.GetReferencingForeignKeys(objectId);
            foreach (var fk in foreignKeys)
            {
                if (connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = fk.ConstraintName }))
                {
                    yield return $"ALTER TABLE [{fk.ReferencingTable.Schema}].[{fk.ReferencingTable.Name}] DROP CONSTRAINT [{fk.ConstraintName}]";
                }
            }
        }

        internal static bool HasColumnName(this Type modelType, string columnName)
        {
            return modelType.GetProperties().Any(pi => pi.SqlColumnName().ToLower().Equals(columnName.ToLower()));
        }
    }
}
