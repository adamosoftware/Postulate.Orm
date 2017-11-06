using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge.Abstract;
using Postulate.Orm.Merge.MergeActions;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge
{
    public class Engine<TScriptGen> where TScriptGen : SqlScriptGenerator, new()
    {
        protected readonly Type[] _modelTypes;
        protected readonly IProgress<MergeProgress> _progress;

        public Engine(Assembly assembly, IProgress<MergeProgress> progress)
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

        public async Task<IEnumerable<MergeAction>> CompareAsync(IDbConnection connection)
        {
            List<MergeAction> results = new List<MergeAction>();

            await Task.Run(() =>
            {
                SyncTablesAndColumns(connection, results);
                DropTables(connection, results);
            });

            return results;
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

            var scriptGen = new TScriptGen();
            var columns = scriptGen.GetSchemaColumns(connection);
            TableInfo tableInfo = null;

            foreach (var type in _modelTypes)
            {
                tableInfo = TableInfo.FromModelType(type, connection: connection);
                counter++;
                _progress?.Report(new MergeProgress()
                {
                    Description = $"Analyzing model class '{type.Name}'...",
                    PercentComplete = PercentComplete(counter, _modelTypes.Length)
                });

                if (!scriptGen.TableExists(connection, type))
                {
                    results.Add(new CreateTable(scriptGen, type));
                    foreignKeys.AddRange(type.GetForeignKeys());
                }
                else
                {
                    var modelColInfo = type.GetModelPropertyInfo(scriptGen);
                    var schemaColInfo = columns[tableInfo.ObjectId];

                    IEnumerable<PropertyInfo> addedColumns;
                    IEnumerable<PropertyInfo> modifiedColumns;
                    IEnumerable<ColumnInfo> deletedColumns;

                    if (AnyColumnsChanged(modelColInfo, schemaColInfo, out addedColumns, out modifiedColumns, out deletedColumns))
                    {
                        if (scriptGen.IsTableEmpty(connection, type))
                        {
                            // drop and re-create table, indicating affected columns with comments in generated script
                            results.Add(new CreateTable(scriptGen, type, rebuild: true)
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
                            results.AddRange(addedColumns.Select(c => new AddColumn(scriptGen, c)));
                            results.AddRange(modifiedColumns.Select(c => new AlterColumn(scriptGen, c)));
                            results.AddRange(deletedColumns.Select(c => new DropColumn(scriptGen, c)));
                            foreignKeys.AddRange(addedColumns.Where(pi => pi.IsForeignKey()));
                        }
                    }

                    // todo: AnyKeysChanged()
                }
            }

            results.AddRange(foreignKeys.Select(fk => new AddForeignKey(scriptGen, fk)));
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

        public StringBuilder GetScript(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
            Dictionary<MergeAction, LineRange> lineRanges;
            return GetScript(connection, actions, out lineRanges);
        }

        public StringBuilder GetScript(IDbConnection connection, IEnumerable<MergeAction> actions, out Dictionary<MergeAction, LineRange> lineRanges)
        {
            lineRanges = new Dictionary<MergeAction, LineRange>();
            int startRange = 0;
            int endRange = 0;

            StringBuilder sb = new StringBuilder();
            var scriptGen = new TScriptGen();

            foreach (var action in actions)
            {
                foreach (var cmd in action.SqlCommands(connection))
                {
                    sb.AppendLine(cmd);
                    sb.AppendLine(scriptGen.CommandSeparator);
                    endRange += GetLineCount(cmd) + 4;
                }

                lineRanges.Add(action, new LineRange(startRange, endRange));
                startRange = endRange;
            }

            return sb;
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

        public async Task ExecuteAsync(IDbConnection connection, IEnumerable<MergeAction> actions)
        {
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
        }

        public async Task ExecuteAsync(IDbConnection connection)
        {
            var actions = await CompareAsync(connection);
            await ExecuteAsync(connection, actions);
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