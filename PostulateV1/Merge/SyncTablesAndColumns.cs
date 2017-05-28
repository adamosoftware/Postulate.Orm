using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge.Action;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb>
    {
        /// <summary>
        /// Derives a list of actions to synchronize the model classes with the database schema
        /// </summary>
        private IEnumerable<MergeAction> SyncTablesAndColumns(IDbConnection connection)
        {
            List<MergeAction> results = new List<MergeAction>();

            var newTables = NewTables(connection);
            results.AddRange(newTables);

            var newColumns = NewColumns(connection, newTables.OfType<CreateTable>());
            var rebuiltTables = AddColumnsWithEmptyTableRebuild(connection, newColumns);
            results.AddRange(rebuiltTables);
            results.AddRange(AddColumnsWithTableAlter(connection, newColumns));

            var deletedTables = DeletedTables(connection);
            results.AddRange(deletedTables.Select(obj => new DropTable(obj)));

            var deletedColumns = DeletedColumns(connection, deletedTables);
            results.AddRange(deletedColumns.Select(cr => new DropColumn(cr, cr.FindModelType(_modelTypes))));

            var newFK = _modelTypes.SelectMany(t =>
                t.GetModelForeignKeys().Where(pi =>
                    (!connection.ForeignKeyExists(pi) && connection.TableExists(t)) ||
                    rebuiltTables.OfType<CreateTable>().Any(ct => ct.ModelType.Equals(pi.GetForeignKeyType()))));
            results.AddRange(newFK.Select(pi => new CreateForeignKey(pi)));

            var deletedFK = DeletedForeignKeys(connection, deletedTables, deletedColumns);
            results.AddRange(deletedFK.Select(fk => new DropForeignKey(fk)));

            return results;
        }

        private IEnumerable<ForeignKeyRef> DeletedForeignKeys(IDbConnection connection, IEnumerable<DbObject> deletedTables, IEnumerable<ColumnRef> deletedColumns)
        {
            var results = GetSchemaFKs(connection).Where(fk =>
                !deletedTables.Any(obj => fk.ChildObject.Equals(obj)) &&
                !deletedColumns.Any(cr => fk.Child.Equals(cr)));

            return results.Where(fk =>
            {
                Type t = FindModelType(fk.ChildObject);
                if (t != null) // will be null in case of renamed table
                {
                    var fkNames = t.GetModelForeignKeys().Select(pi => pi.ForeignKeyName().ToLower());
                    return !fkNames.Contains(fk.ConstraintName.ToLower());
                }
                return false;
            });
        }

        private IEnumerable<ForeignKeyRef> GetSchemaFKs(IDbConnection connection)
        {
            return connection.Query<ForeignKeyInfo>(
                @"SELECT
                    [fk].[name] AS [ConstraintName],
                    SCHEMA_NAME([t].[schema_id]) AS [ReferencingSchema],
                    [t].[name] AS [ReferencingTable],
                    [col].[name] AS [ReferencingColumn]
                FROM
                    [sys].[foreign_key_columns] [fkcol] INNER JOIN [sys].[columns] [col] ON
						[fkcol].[parent_object_id]=[col].[object_id] AND
						[fkcol].[parent_column_id]=[col].[column_id]
					INNER JOIN [sys].[foreign_keys] [fk] ON [fkcol].[constraint_object_id]=[fk].[object_id]
					INNER JOIN [sys].[tables] [t] ON [fkcol].[parent_object_id]=[t].[object_id]")
                    .Select(row => new ForeignKeyRef()
                    {
                        ConstraintName = row.ConstraintName,
                        Child = new ColumnRef() { ColumnName = row.ReferencingColumn, Schema = row.ReferencingSchema, TableName = row.ReferencingTable },
                        ChildObject = new DbObject(row.ReferencingSchema, row.ReferencingTable)
                    });
        }

        private IEnumerable<ColumnRef> DeletedColumns(IDbConnection connection, IEnumerable<DbObject> deletedTables)
        {
            var modelColumns = GetModelColumns(connection);

            return GetSchemaColumns(connection).Where(sc =>
                !modelColumns.Any(
                    mc => mc.Equals(sc) ||
                    (mc.PropertyInfo.GetAttribute<RenameFromAttribute>()?.OldName?.Equals(sc.ColumnName) ?? false) ||
                    (mc.PropertyInfo.DeclaringType.HasAttribute<RenameFromAttribute>())) &&
                !deletedTables.Contains(new DbObject(sc.Schema, sc.TableName)));
        }

        private IEnumerable<DbObject> DeletedTables(IDbConnection connection)
        {
            var schemaTables = GetSchemaTables(connection);
            return schemaTables.Where(obj => !_modelTypes.Any(t => obj.Equals(t) || obj.Equals(t.GetAttribute<RenameFromAttribute>())));
        }

        private IEnumerable<MergeAction> AddColumnsWithTableAlter(IDbConnection connection, IEnumerable<PropertyInfo> newColumns)
        {
            // tables with data may only have columns added
            var alterColumns = newColumns
                .Where(pi => !pi.DeclaringType.IsAbstract)
                .GroupBy(pi => pi.GetDbObject(connection))
                .Where(obj => connection.TableExists(obj.Key.Schema, obj.Key.Name) && !connection.IsTableEmpty(obj.Key.Schema, obj.Key.Name))
                .SelectMany(tbl => tbl);

            // adding primary key columns requires containing PKs to be rebuilt
            var pkColumns = alterColumns.Where(pi => pi.HasAttribute<PrimaryKeyAttribute>());
            var pkTables = pkColumns.GroupBy(item => DbObject.FromType(item.DeclaringType)).Select(grp => grp.Key);
            foreach (var pk in pkTables)
            {
                yield return new DropPrimaryKey(pk);
            }

            // todo: same thing with unique keys -- they must be rebuilt if column additions impact them

            foreach (var col in alterColumns) yield return new AddColumn(col);

            foreach (var pk in pkTables)
            {
                yield return new CreatePrimaryKey(pk);
            }
        }

        private IEnumerable<CreateTable> AddColumnsWithEmptyTableRebuild(IDbConnection connection, IEnumerable<PropertyInfo> newColumns)
        {
            // empty tables may be dropped and rebuilt with new columns
            var rebuildTables = newColumns
                .GroupBy(pi => pi.GetDbObject(connection))
                .Where(obj => connection.TableExists(obj.Key.Schema, obj.Key.Name) && connection.IsTableEmpty(obj.Key.Schema, obj.Key.Name));

            foreach (var tbl in rebuildTables)
            {
                var addedColumns = tbl.Select(pi => pi.SqlColumnName());
                yield return new CreateTable(tbl.Key.ModelType, true, addedColumns);
            }
        }

        private IEnumerable<PropertyInfo> NewColumns(IDbConnection connection, IEnumerable<CreateTable> newTables)
        {
            var schemaColumns = GetSchemaColumns(connection);

            return _modelTypes.Where(t => !newTables.Any(ct => ct.Equals(t)))
                .SelectMany(t => t.GetProperties()
                    .Where(pi =>
                        !schemaColumns.Any(cr => cr.Equals(pi)) &&
                        !connection.ColumnExists(t.GetSchema(), t.GetTableName(), pi.SqlColumnName()) &&
                        pi.CanWrite &&
                        IsSupportedType(pi.PropertyType) &&
                        !pi.Name.ToLower().Equals(nameof(Record<int>.Id).ToLower()) &&
                        !pi.HasAttribute<RenameFromAttribute>() &&
                        !pi.HasAttribute<NotMappedAttribute>()));
        }

        private IEnumerable<MergeAction> NewTables(IDbConnection connection)
        {
            var schemaTables = GetSchemaTables(connection);
            var addTables = _modelTypes.Where(t => !t.HasAttribute<RenameFromAttribute>() && !schemaTables.Any(st => st.Equals(t)));
            return addTables.Select(t => new CreateTable(t));
        }
    }
}