using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge.Actions;
using Postulate.Orm.Merge.Extensions;
using Postulate.Orm.Merge.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge
{
    public abstract class Engine
    {
        protected readonly Type[] _modelTypes;
        protected readonly IProgress<CompareProgress> _progress;

        public Engine(Assembly assembly, IProgress<CompareProgress> progress)
        {
            _modelTypes = assembly.GetTypes()
                .Where(t =>
                    !t.Name.StartsWith("<>") &&
                    !t.HasAttribute<NotMappedAttribute>() &&
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    t.IsDerivedFromGeneric(typeof(Record<>))).ToArray();

            _progress = progress;
        }

        public async Task<IEnumerable<Action2>> CompareAsync(IDbConnection connection)
        {
            List<Action2> results = new List<Action2>();

            await Task.Run(() =>
            {
                SyncTablesAndColumns(connection, results);                
                DropTables(connection, results);                
            });

            _progress?.Report(new CompareProgress() { Description = "Finished" });

            return results;
        }

        private void DropTables(IDbConnection connection, List<Action2> results)
        {
            _progress?.Report(new CompareProgress() { Description = "Looking for deleted tables..." });

            //throw new NotImplementedException();
        }

        private void SyncTablesAndColumns(IDbConnection connection, List<Action2> results)
        {
            int counter = 0;
            List<PropertyInfo> foreignKeys = new List<PropertyInfo>();

            foreach (var type in _modelTypes)
            {
                counter++;
                _progress?.Report(new CompareProgress()
                {
                    Description = $"Analyzing model class '{type.Name}'...",
                    PercentComplete = PercentComplete(counter, _modelTypes.Length)
                });

                if (!TableExists(connection, type))
                {
                    results.Add(new CreateTable(type));
                    foreignKeys.AddRange(type.GetForeignKeys());
                }
                else
                {
                    var modelColInfo = type.GetModelPropertyInfo();
                    var schemaColInfo = GetSchemaColumns(connection, type);

                    IEnumerable<PropertyInfo> addedColumns;
                    IEnumerable<PropertyInfo> modifiedColumns;
                    IEnumerable<ColumnInfo> deletedColumns;

                    if (AnyColumnsChanged(modelColInfo, schemaColInfo, out addedColumns, out modifiedColumns, out deletedColumns))
                    {
                        if (IsTableEmpty(connection, type))
                        {
                            // drop and re-create table, indicating affected columns with comments in generated script
                            results.Add(new CreateTable(type, rebuild: true)
                            {
                                AddedColumns = addedColumns.Select(pi => pi.SqlColumnName()),
                                ModifiedColumns = modifiedColumns.Select(pi => pi.SqlColumnName()),
                                DeletedColumns = deletedColumns.Select(sc => sc.ColumnName)
                            });
                            foreignKeys.AddRange(type.GetForeignKeys());
                        }
                        else
                        {
                            // make changes to the table without dropping it
                            results.AddRange(addedColumns.Select(c => new AddColumn(c)));
                            results.AddRange(modifiedColumns.Select(c => new AlterColumn(c)));
                            results.AddRange(deletedColumns.Select(c => new DropColumn(c)));
                            foreignKeys.AddRange(addedColumns.Where(pi => pi.IsForeignKey()));
                        }
                    }

                    // todo: AnyKeysChanged()
                }
            }

            results.AddRange(foreignKeys.Select(fk => new AddForeignKey(fk)));
        }

        private bool AnyColumnsChanged(
            IEnumerable<PropertyInfo> modelPropertyInfo, IEnumerable<ColumnInfo> schemaColumnInfo,
            out IEnumerable<PropertyInfo> addedColumns, out IEnumerable<PropertyInfo> modifiedColumns, out IEnumerable<ColumnInfo> deletedColumns)
        {
            addedColumns = modelPropertyInfo.Where(pi => !schemaColumnInfo.Contains(pi.ToColumnInfo()));

            modifiedColumns = from mp in modelPropertyInfo
                              join sc in schemaColumnInfo on mp.ToColumnInfo() equals sc
                              where mp.ToColumnInfo().IsAlteredFrom(sc)
                              select mp;

            deletedColumns = schemaColumnInfo.Where(sc => !modelPropertyInfo.Select(pi => pi.ToColumnInfo()).Contains(sc));

            return (addedColumns.Any() || modifiedColumns.Any() || deletedColumns.Any());
        }

        private static bool HasColumnName(Type modelType, string columnName)
        {
            return modelType.GetProperties().Any(pi => pi.SqlColumnName().ToLower().Equals(columnName.ToLower()));
        }

        private IEnumerable<ColumnInfo> GetSchemaColumns(IDbConnection connection, Type type)
        {
            var results = connection.Query<ColumnInfo>(SchemaColumnQuery, SchemaColumnParameters(type));
            // todo exclude select schemas
            return results;
        }

        protected abstract string GetTableName(Type type);

        protected abstract string ApplyDelimiter(string objectName);

        protected abstract string IsTableEmptyQuery { get; }

        protected abstract string TableExistsQuery { get; }

        protected abstract object TableExistsParameters(Type type);

        protected abstract string ColumnExistsQuery { get; }

        protected abstract object ColumnExistsParameters(PropertyInfo propertyInfo);

        protected abstract string SchemaColumnQuery { get; }

        protected abstract object SchemaColumnParameters(Type type);

        protected bool IsTableEmpty(IDbConnection connection, Type t)
        {
            //$"SELECT COUNT(1) FROM [{schema}].[{tableName}]"
            return ((connection.QueryFirstOrDefault<int?>(IsTableEmptyQuery, null) ?? 0) == 0);
        }

        protected bool TableExists(IDbConnection connection, Type t)
        {
            //return connection.Exists("[sys].[tables] WHERE SCHEMA_NAME([schema_id])=@schema AND [name]=@name", new { schema = schema, name = tableName });
            return connection.Exists(TableExistsQuery, TableExistsParameters(t));
        }

        private bool ColumnExists(IDbConnection connection, PropertyInfo pi)
        {
            return connection.Exists(ColumnExistsQuery, ColumnExistsParameters(pi));
        }

        private int PercentComplete(int value, int total)
        {
            return Convert.ToInt32((Convert.ToDouble(value) / Convert.ToDouble(total)) * 100);
        }
    }
}