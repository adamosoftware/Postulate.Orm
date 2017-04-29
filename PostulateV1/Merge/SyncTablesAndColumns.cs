using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Extensions;
using Postulate.Merge.Action;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
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
            results.AddRange(AddColumnsWithEmptyTableRebuild(connection, newColumns));
            results.AddRange(AddColumnsWithTableAlter(connection, newColumns));

            var deletedTables = DeletedTables(connection);
            results.AddRange(deletedTables.Select(obj => new DropTable(obj)));

            var deletedColumns = DeletedColumns(connection, deletedTables);
            results.AddRange(deletedColumns.Select(cr => new DropColumn(cr, cr.FindModelType(_modelTypes))));

            var foreignKeys = _modelTypes.SelectMany(t => t.GetModelForeignKeys().Where(pi => !connection.ForeignKeyExists(pi)));
            results.AddRange(foreignKeys.Select(pi => new CreateForeignKey(pi)));

            return results;
        }

        private IEnumerable<ColumnRef> DeletedColumns(IDbConnection connection, IEnumerable<DbObject> deletedTables)
        {
            var modelColumns = GetModelColumns(connection);

            return GetSchemaColumns(connection).Where(sc =>
                !modelColumns.Any(mc => mc.Equals(sc)) &&
                !deletedTables.Contains(new DbObject(sc.Schema, sc.TableName)));
        }

        private IEnumerable<DbObject> DeletedTables(IDbConnection connection)
        {
            var schemaTables = GetSchemaTables(connection);
            return schemaTables.Where(obj => !_modelTypes.Any(t => obj.Equals(t)));
        }

        private IEnumerable<MergeAction> AddColumnsWithTableAlter(IDbConnection connection, IEnumerable<PropertyInfo> newColumns)
        {
            // tables with data may only have columns added
            var alterColumns = newColumns
                .GroupBy(pi => pi.GetDbObject(connection))
                .Where(obj => !connection.IsTableEmpty(obj.Key.Schema, obj.Key.Name))
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

        private IEnumerable<MergeAction> AddColumnsWithEmptyTableRebuild(IDbConnection connection, IEnumerable<PropertyInfo> newColumns)
        {
            // empty tables may be dropped and rebuilt with new columns
            var rebuildTables = newColumns
                .GroupBy(pi => pi.GetDbObject(connection))
                .Where(obj => connection.IsTableEmpty(obj.Key.Schema, obj.Key.Name));

            foreach (var tbl in rebuildTables)
            {
                yield return new DropTable(tbl.Key, $"Empty table being re-created to add new columns: {string.Join(", ", tbl.Select(pi => pi.SqlColumnName()))}");
                yield return new CreateTable(tbl.Key.ModelType);
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
                        !pi.HasAttribute<NotMappedAttribute>()));
        }

        private IEnumerable<MergeAction> NewTables(IDbConnection connection)
        {
            var schemaTables = GetSchemaTables(connection);
            var addTables = _modelTypes.Where(t => !schemaTables.Any(st => st.Equals(t)));
            return addTables.Select(t => new CreateTable(t));
        }
    }    
}
