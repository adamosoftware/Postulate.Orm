using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge
{
    internal static class Helpers
    {
        internal static IEnumerable<ForeignKeyRef> GetReferencingForeignKeys(this IDbConnection connection, int objectID)
        {
            return connection.Query<ForeignKeyInfo>(
                @"SELECT
                    [fk].[name] AS [ConstraintName],
                    SCHEMA_NAME([parent].[schema_id]) AS [ReferencedSchema],
                    [parent].[name] AS [ReferencedTable],
                    [refdcol].[name] AS [ReferencedColumn],
                    SCHEMA_NAME([child].[schema_id]) AS [ReferencingSchema],
                    [child].[name] AS [ReferencingTable],
                    [rfincol].[name] AS [ReferencingColumn]
                FROM
                    [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [child] ON [fk].[parent_object_id]=[child].[object_id]
                    INNER JOIN [sys].[tables] [parent] ON [fk].[referenced_object_id]=[parent].[object_id]
                    INNER JOIN [sys].[foreign_key_columns] [fkcol] ON
                        [fk].[parent_object_id]=[fkcol].[parent_object_id] AND
                        [fk].[object_id]=[fkcol].[constraint_object_id]
                    INNER JOIN [sys].[columns] [refdcol] ON
                        [fkcol].[referenced_column_id]=[refdcol].[column_id] AND
                        [fkcol].[referenced_object_id]=[refdcol].[object_id]
                    INNER JOIN [sys].[columns] [rfincol] ON
                        [fkcol].[parent_column_id]=[rfincol].[column_id] AND
                        [fkcol].[parent_object_id]=[rfincol].[object_id]
				WHERE
                    [fk].[referenced_object_id]=@objID", new { objID = objectID })
                .Select(fk => new ForeignKeyRef()
                {
                    ConstraintName = fk.ConstraintName,
                    Child = new ColumnRef() { Schema = fk.ReferencingSchema, TableName = fk.ReferencingTable, ColumnName = fk.ReferencingColumn },
                    Parent = new ColumnRef() { Schema = fk.ReferencedSchema, TableName = fk.ReferencedTable, ColumnName = fk.ReferencedColumn }
                });
        }

        internal static IEnumerable<PropertyInfo> GetModelForeignKeys(this Type modelType)
        {
            List<string> temp = new List<string>();
            foreach (var pi in modelType.GetProperties().Where(pi => pi.HasAttribute<ForeignKeyAttribute>()))
            {
                temp.Add(pi.Name.ToLower());
                yield return pi;
            }

            foreach (var attr in modelType.GetCustomAttributes<ForeignKeyAttribute>()
                .Where(attr => HasColumnName(modelType, attr.ColumnName) && !temp.Contains(attr.ColumnName.ToLower())))
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
                new ForeignKeyRef() { ConstraintName = pi.ForeignKeyName(), ChildObject = DbObject.FromType(pi.DeclaringType), PropertyInfo = pi }
            ));
        }

        internal static IEnumerable<string> GetFKDropStatements(this IDbConnection connection, int objectId)
        {
            var foreignKeys = connection.GetReferencingForeignKeys(objectId);
            foreach (var fk in foreignKeys)
            {
                if (connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = fk.ConstraintName }))
                {
                    yield return $"ALTER TABLE [{fk.Child.Schema}].[{fk.Child.TableName}] DROP CONSTRAINT [{fk.ConstraintName}]";
                }
            }
        }

        internal static bool HasColumnName(this Type modelType, string columnName)
        {
            return modelType.GetProperties().Any(pi => pi.SqlColumnName().ToLower().Equals(columnName.ToLower()));
        }
    }
}