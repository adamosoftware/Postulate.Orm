using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.ModelMerge.Actions;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.ModelMerge
{
    public class Engine<TSyntax> where TSyntax : SqlSyntax, new()
    {
        protected readonly Type[] _modelTypes;
        protected readonly IProgress<MergeProgress> _progress;
        protected readonly TSyntax _syntax;

        private Stopwatch _stopwatch = null;

        public Engine(Type[] modelTypes, IProgress<MergeProgress> progress = null)
        {
            if (modelTypes.Any(t => !t.IsDerivedFromGeneric(typeof(Record<>))))
            {
                throw new ArgumentException("Model types used with Postulate.Orm.ModelMerge.Engine must all derive from Postulate.Orm.Abstract.Record<>");
            }            

            _modelTypes = modelTypes;
            _progress = progress;
            _syntax = new TSyntax();
        }

        public Engine(Assembly assembly, IProgress<MergeProgress> progress = null) : this(GetModelTypes(assembly), progress)
        {
        }

        public static Type[] GetModelTypes(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(t =>
                    !t.Name.StartsWith("<>") &&                    
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    t.IsDerivedFromGeneric(typeof(Record<>))).ToArray();
        }

        public TSyntax Syntax { get { return _syntax; } }

        public Stopwatch Stopwatch { get { return _stopwatch; } }

        public async Task<IEnumerable<MergeAction>> CompareAsync(IDbConnection connection)
        {
            List<MergeAction> results = new List<MergeAction>();

            _stopwatch = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                SyncTablesAndColumns(connection, results);
                DropTables(connection, results);
            });

            _stopwatch.Stop();

            return results;
        }

        public StringBuilder GetScript(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            Dictionary<MergeAction, LineRange> lineRanges;
            return GetScript(connection, actions, out lineRanges);
        }

        public StringBuilder GetScript(IDbConnection connection, IEnumerable<MergeAction> actions, out Dictionary<MergeAction, LineRange> lineRanges)
        {
            _stopwatch = Stopwatch.StartNew();

            lineRanges = new Dictionary<MergeAction, LineRange>();
            int startRange = 0;
            int endRange = 0;

            StringBuilder sb = new StringBuilder();
            var syntax = new TSyntax();

            foreach (var action in actions)
            {
                sb.AppendLine($"{Syntax.CommentPrefix} {action.Description}");

                var errors = action.ValidationErrors(connection);
                if (errors.Any())
                {
                    sb.AppendLine($"{Syntax.CommentPrefix} One or more validation errors were found:");
                    foreach (var err in action.ValidationErrors(connection))
                    {
                        sb.AppendLine($"{Syntax.CommentPrefix} {err}");
                    }
                }

                foreach (var cmd in action.SqlCommands(connection))
                {
                    sb.AppendLine(cmd);
                    sb.AppendLine(syntax.CommandSeparator);
                    endRange += GetLineCount(cmd) + 4;
                }

                lineRanges.Add(action, new LineRange(startRange, endRange));
                startRange = endRange;
            }

            _stopwatch.Stop();

            return sb;
        }

        public void SaveScript(string fileName, IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            using (StreamWriter writer = File.CreateText(fileName))
            {
                writer.Write(GetScript(connection, actions));
            }
        }

        public async Task ExecuteAsync(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            _stopwatch = Stopwatch.StartNew();

            Validate(connection, actions);

            int index = 0;
            int max = actions.Count();

            foreach (var diff in actions)
            {
                index++;
                _progress?.Report(new MergeProgress() { Description = diff.Description, PercentComplete = PercentComplete(index, max) });

                foreach (var cmd in diff.SqlCommands(connection))
                {
                    await connection.ExecuteAsync(cmd);
                }
            }

            _stopwatch.Stop();
            _progress?.Report(new MergeProgress() { Description = $"Execute ran in {_stopwatch.ElapsedMilliseconds}ms", PercentComplete = 100 });
        }

        public async Task ExecuteAsync(IDbConnection connection)
        {
            _stopwatch = Stopwatch.StartNew();

            var actions = await CompareAsync(connection);
            await ExecuteAsync(connection, actions);

            _stopwatch.Stop();
            _progress?.Report(new MergeProgress() { Description = $"Compare + Execute ran in {_stopwatch.ElapsedMilliseconds}ms", PercentComplete = 100 });
        }

        private void DropTables(IDbConnection connection, List<MergeAction> results)
        {
            _progress?.Report(new MergeProgress() { Description = "Looking for deleted tables..." });

            //throw new NotImplementedException();
        }

        private void SyncTablesAndColumns(IDbConnection connection, List<MergeAction> results)
        {
            int counter = 0;
            List<PropertyInfo> foreignKeys = new List<PropertyInfo>();

            _progress?.Report(new MergeProgress() { Description = "Getting column info...", PercentComplete = 0 });
            var columns = _syntax.GetSchemaColumns(connection);

            TableInfo tableInfo = null;

            _progress?.Report(new MergeProgress()
            {
                Description = "Analzying schemas...",
                PercentComplete = 0
            });

            var schemas = _modelTypes.Select(t => Syntax.GetTableInfoFromType(t).Schema).GroupBy(s => s).Select(grp => grp.Key);
            CreateSchemas(connection, results, schemas);

            _progress?.Report(new MergeProgress()
            {
                Description = "Analzying enum foreign keys...",
                PercentComplete = 0
            });

            var enumTableSchemas = _modelTypes
                .SelectMany(t => t.GetProperties().Where(pi => IsEnumForeignKey(pi.PropertyType)))
                .Select(pi => pi.PropertyType)
                .GroupBy(t => t.GetAttribute<EnumTableAttribute>().Schema)
                .Select(grp => grp.Key)
                .Where(s => !string.IsNullOrEmpty(s));
            CreateSchemas(connection, results, enumTableSchemas);

            var enumTables = _modelTypes
                .SelectMany(t => t.GetProperties().Where(pi => IsEnumForeignKey(pi.PropertyType)))
                .Select(pi => pi.PropertyType)
                .GroupBy(t => t.GetAttribute<EnumTableAttribute>().FullTableName())
                .Select(grp => grp.First());
            results.AddRange(enumTables.Select(enumType => new CreateEnumTable(Syntax, enumType)));

            foreach (var type in _modelTypes)
            {
                counter++;
                _progress?.Report(new MergeProgress()
                {
                    Description = $"Analyzing model class '{type.Name}'...",
                    PercentComplete = PercentComplete(counter, _modelTypes.Length)
                });

                tableInfo = _syntax.GetTableInfoFromType(type);

                if (!_syntax.TableExists(connection, type))
                {
                    results.Add(new CreateTable(_syntax, tableInfo));
                    foreignKeys.AddRange(type.GetForeignKeys().Where(fk => _modelTypes.Contains(fk.GetForeignKeyParentType())));                    
                }
                else
                {
                    if (!_syntax.FindObjectId(connection, tableInfo)) throw new Exception($"Couldn't find Object Id for table {tableInfo}.");

                    var modelColInfo = type.GetModelPropertyInfo(_syntax);
                    var schemaColInfo = columns[tableInfo.ObjectId].ToList();

                    // if this is not a table we're allowed to create or drop (such as typically AspNetUsers)...
                    if (!AllowTableCreate(type))
                    {
                        // ...then it means we have to exclude its columns that don't have model equivalents
                        // because those "schema-only" are not meant to be synchronized with model class
                        var schemaOnlyColumns = schemaColInfo.Where(col => !modelColInfo.Any(pi => pi.SqlColumnName().Equals(col.ColumnName))).ToArray();
                        foreach (ColumnInfo column in schemaOnlyColumns) schemaColInfo.Remove(column);
                    }

                    IEnumerable<PropertyInfo> addedColumns;
                    IEnumerable<AlterColumn> modifiedColumns;
                    IEnumerable<ColumnInfo> deletedColumns;

                    if (AnyColumnsChanged(modelColInfo, schemaColInfo, out addedColumns, out modifiedColumns, out deletedColumns))
                    {
                        if (_syntax.IsTableEmpty(connection, type) && AllowTableCreate(type))
                        {
                            // drop and re-create table, indicating affected columns with comments in generated script
                            results.Add(new CreateTable(_syntax, tableInfo, rebuild: true)
                            {
                                AddedColumns = addedColumns.Select(pi => pi.SqlColumnName()),
                                ModifiedColumns = modifiedColumns.Select(ac => ac.ColumnName),
                                DeletedColumns = deletedColumns.Select(sc => sc.ColumnName)
                            });
                            foreignKeys.AddRange(type.GetForeignKeys().Where(fk => _modelTypes.Contains(fk.GetForeignKeyParentType())));                            
                        }
                        else
                        {
                            // make changes to the table without dropping it
                            results.AddRange(addedColumns.Select(c => new AddColumn(_syntax, c)));
                            if (AllowTableCreate(type))
                            {
                                // when we aren't allow to create/drop the table, then I don't allow column alter or dropping either
                                // this is a hack for interacting with AspNetUsers table
                                results.AddRange(modifiedColumns);
                                results.AddRange(deletedColumns.Select(c => new DropColumn(_syntax, c)));
                            }
                            foreignKeys.AddRange(addedColumns.Where(pi => pi.IsForeignKey() && _modelTypes.Contains(pi.GetForeignKeyParentType())));
                        }
                    }

                    IEnumerable<PropertyInfo> addedForeignKeys;
                    IEnumerable<ColumnInfo> deletedForeignKeys;
                    if (AnyForeignKeysChanged(modelColInfo, schemaColInfo, out addedForeignKeys, out deletedForeignKeys))
                    {
                        foreignKeys.AddRange(addedForeignKeys);
                        if (AllowTableCreate(type))
                        {
                            results.AddRange(deletedForeignKeys.Select(colInfo => new DropForeignKey(Syntax, colInfo)));
                        }
                    }

                    // todo: AnyKeysChanged()
                }

                foreignKeys.AddRange(type.GetProperties().Where(pi => IsEnumForeignKey(pi.PropertyType)));
            }

            results.AddRange(foreignKeys.Select(fk => new AddForeignKey(_syntax, fk)));
        }

        private bool IsEnumForeignKey(Type propertyType)
        {
            return propertyType.IsEnum && propertyType.HasAttribute<EnumTableAttribute>();
        }

        private void CreateSchemas(IDbConnection connection, List<MergeAction> results, IEnumerable<string> schemas)
        {
            foreach (var schema in schemas)
            {
                if (!Syntax.SchemaExists(connection, schema))
                {
                    results.Add(new CreateSchema(_syntax, schema));
                }
            }
        }

        private bool AllowTableCreate(Type modelType)
        {
            return !modelType.HasAttribute<NotMappedAttribute>();
        }

        private bool AnyForeignKeysChanged(IEnumerable<PropertyInfo> modelColInfo, IEnumerable<ColumnInfo> schemaColInfo, out IEnumerable<PropertyInfo> addedForeignKeys, out IEnumerable<ColumnInfo> deletedForeignKeys)
        {
            addedForeignKeys = from mc in modelColInfo
                               join sc in schemaColInfo on mc.SqlColumnName() equals sc.ColumnName
                               where mc.IsForeignKey() && !sc.IsForeignKey
                               select mc;

            deletedForeignKeys = from sc in schemaColInfo
                                 join mc in modelColInfo on sc.ColumnName equals mc.SqlColumnName()
                                 where sc.IsForeignKey && !mc.IsForeignKey()
                                 select sc;

            return (addedForeignKeys.Any() || deletedForeignKeys.Any());
        }

        private bool AnyColumnsChanged(
            IEnumerable<PropertyInfo> modelPropertyInfo, IEnumerable<ColumnInfo> schemaColumnInfo,
            out IEnumerable<PropertyInfo> addedColumns, out IEnumerable<AlterColumn> modifiedColumns, out IEnumerable<ColumnInfo> deletedColumns)
        {
            addedColumns = modelPropertyInfo.Where(pi => !schemaColumnInfo.Contains(pi.ToColumnInfo(Syntax)));

            modifiedColumns = (from mp in modelPropertyInfo
                              join sc in schemaColumnInfo on mp.SqlColumnName() equals sc.ColumnName
                              where mp.ToColumnInfo(Syntax).IsAlteredFrom(sc)
                              select new AlterColumn(Syntax, sc, mp)).ToList();

            deletedColumns = schemaColumnInfo.Where(sc => !modelPropertyInfo.Select(pi => pi.ToColumnInfo(Syntax)).Contains(sc));

            return (addedColumns.Any() || modifiedColumns.Any() || deletedColumns.Any());
        }

        private static bool HasColumnName(Type modelType, string columnName)
        {
            return modelType.GetProperties().Any(pi => pi.SqlColumnName().ToLower().Equals(columnName.ToLower()));
        }

        public void Validate(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            if (actions.Any(a => !a.IsValid(connection)))
            {
                string message = string.Join("\r\n", ValidationErrors(connection, actions));
                throw new ValidationException($"The model has one or more validation errors:\r\n{message}");
            }
        }

        public static IEnumerable<ValidationError> ValidationErrors(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            return actions.Where(a => !a.IsValid(connection)).SelectMany(a => a.ValidationErrors(connection), (a, m) => new ValidationError(a, m));
        }

        private int GetLineCount(string text)
        {
            int result = 0;
            int start = 0;
            while (true)
            {
                int position = text.IndexOf("\r\n", start);
                if (position == -1) break;
                start = position + 1;
                result++;
            }
            return result;
        }

        private int PercentComplete(int value, int total)
        {
            return Convert.ToInt32((Convert.ToDouble(value) / Convert.ToDouble(total)) * 100);
        }

        public class ValidationError
        {
            private readonly MergeAction _action;
            private readonly string _message;

            public ValidationError(MergeAction action, string message)
            {
                _action = action;
                _message = message;
            }

            public MergeAction Action => _action;

            public override string ToString()
            {
                return $"{_action.ToString()}: {_message}";
            }
        }
    }
}