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

namespace Postulate.Orm.Merge
{
    public abstract class Engine
    {
        protected readonly IEnumerable<Type> _modelTypes;
        protected readonly int _modelTypeCount;
        protected readonly IProgress<string> _stepProgress;
        protected readonly IProgress<Progress> _objectProgress;

        public Engine(IEnumerable<Type> modelTypes, IProgress<string> stepProgress, IProgress<Progress> objectProgress)
        {
            _modelTypes = modelTypes;
            _modelTypeCount = modelTypes.Count();
            _stepProgress = stepProgress;
            _objectProgress = objectProgress;
        }

        public async Task<IEnumerable<Action2>> CompareAsync(IDbConnection connection)
        {
            List<Action2> results = new List<Action2>();

            await Task.Run(() =>
            {
                int typeCounter = 0;
                foreach (var type in _modelTypes)
                {
                    _stepProgress?.Report("Analyzing model classes...");
                    typeCounter++;
                    _objectProgress?.Report(new Progress() { Description = type.Name, PercentComplete = PercentComplete(typeCounter, _modelTypeCount) });

                    if (!TableExists(connection, type))
                    {
                        results.Add(new CreateTable(type));
                    }
                    else
                    {
                        var modelProps = type.GetModelProperties();
                        var schemaCols = GetSchemaColumns(connection, type).ToDictionary(item => item.ColumnName);
                        foreach (var pi in modelProps)
                        {
                            _stepProgress?.Report("Analyzing properties...");

                            if (!ColumnExists(connection, pi))
                            {
                                results.Add(new AddColumn(pi));
                            }
                            else if (ColumnSignatureChanged(connection, pi, schemaCols))
                            {

                            }
                        }
                    }
                }


            });

            return results;
        }

        private bool ColumnSignatureChanged(IDbConnection connection, PropertyInfo pi, Dictionary<string, ColumnInfo> schemaCols)
        {
            string columnName = pi.SqlColumnName();
            if (schemaCols.ContainsKey(columnName))
            {
                return schemaCols[columnName].SignatureChanged(pi);
            }
            return false;
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