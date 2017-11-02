using Dapper;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge.Actions;
using Postulate.Orm.Merge.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using ReflectionHelper;
using Postulate.Orm.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using Postulate.Orm.Abstract;
using Postulate.Orm.Merge.Models;

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
                _progress?.Report(new CompareProgress() { Description = "Analyzing foreign keys..." });
                var foreignKeys = _modelTypes.SelectMany(t => GetModelForeignKeys(t));                
                int syncObjectCount = _modelTypes.Length + foreignKeys.Count();

                //var droppedTables = 

                SyncTablesAndColumns(connection, results, syncObjectCount);

                SyncForeignKeys(connection, foreignKeys, results, syncObjectCount);

                _progress?.Report(new CompareProgress() { Description = "Looking for deleted tables..." });
                DropTables(connection, results);
            });

            return results;
        }

        private void SyncForeignKeys(IDbConnection connection, IEnumerable<PropertyInfo> foreignKeys, List<Action2> results, int totalObjects)
        {
            foreach (var fk in foreignKeys)
            {

            }
        }

        private void DropTables(IDbConnection connection, List<Action2> results)
        {
            //throw new NotImplementedException();
        }

        private void SyncTablesAndColumns(IDbConnection connection, List<Action2> results, int totalObjects)
        {
            int counter = 0;

            foreach (var type in _modelTypes)
            {
                counter++;
                _progress?.Report(new CompareProgress()
                {
                    Description = $"Analyzing model class '{type.Name}'...",
                    PercentComplete = PercentComplete(counter, totalObjects)
                });

                if (!TableExists(connection, type))
                {
                    results.Add(new CreateTable(type));
                }
                else
                {
                    var modelProps = type.GetModelProperties();
                    var schemaCols = GetSchemaColumns(connection, type);

                    IEnumerable<PropertyInfo> addedColumns;
                    IEnumerable<PropertyInfo> modifiedColumns;
                    IEnumerable<PropertyInfo> deletedColumns;

                    if (AnyColumnsChanged(modelProps, schemaCols, out addedColumns, out modifiedColumns, out deletedColumns))
                    {
                        if (IsTableEmpty(connection, type))
                        {
                            // drop and re-create table, indicating affected columns with comments in generated script
                            results.Add(new CreateTable(type, rebuild: true)
                            {
                                AddedColumns = addedColumns.Select(pi => pi.SqlColumnName()),
                                ModifiedColumns = modifiedColumns.Select(pi => pi.SqlColumnName()),
                                DeletedColumns = deletedColumns.Select(pi => pi.SqlColumnName())
                            });
                        }
                        else
                        {
                            // make changes to the table without dropping it
                            results.AddRange(addedColumns.Select(c => new AddColumn(c)));
                            results.AddRange(modifiedColumns.Select(c => new AlterColumn(c)));
                            results.AddRange(deletedColumns.Select(c => new DropColumn(c)));
                        }
                    }
                }
            }
        }

        private bool AnyColumnsChanged(
            IEnumerable<PropertyInfo> modelProps, IEnumerable<ColumnInfo> schemaCols, 
            out IEnumerable<PropertyInfo> addedColumns, out IEnumerable<PropertyInfo> modifiedColumns, out IEnumerable<PropertyInfo> deletedColumns)
        {
            addedColumns = modelProps.Where(mp => !schemaCols.Any(sc => sc.Equals(mp)));

            throw new NotImplementedException();

            modifiedColumns = from mp in modelProps
                              join sc in schemaCols on mp.SqlColumnName() equals sc.ColumnName
                              where !mp.SqlColumnSyntax().Equals(sc)
                              select mp;
            
        }

        private static IEnumerable<PropertyInfo> GetModelForeignKeys(Type modelType)
        {
            List<string> temp = new List<string>();
            foreach (var pi in modelType.GetProperties().Where(pi => pi.HasAttribute<Attributes.ForeignKeyAttribute>()))
            {
                temp.Add(pi.Name.ToLower());
                yield return pi;
            }

            foreach (var attr in modelType.GetCustomAttributes<Attributes.ForeignKeyAttribute>()
                .Where(attr => HasColumnName(modelType, attr.ColumnName) && !temp.Contains(attr.ColumnName.ToLower())))
            {
                PropertyInfo pi = modelType.GetProperty(attr.ColumnName);
                if (pi != null) yield return pi;
            }
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

        private int PercentComplete(int counter, int max)
        {
            return Convert.ToInt32((Convert.ToDouble(counter) / Convert.ToDouble(max)) * 100);
        }
    }
}