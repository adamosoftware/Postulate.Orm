using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;
using Postulate.Orm.Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using Postulate.Orm.Exceptions;
using Dapper;
using System.Configuration;
using System.Linq.Expressions;
using Postulate.Orm.Interfaces;
using ReflectionHelper;
using Postulate.Orm.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Postulate.Orm.Abstract
{
    public enum ConnectionSource
    {
        ConfigFile,
        Literal
    }

    /// <summary>
    /// Supports CRUD actions for model classes
    /// </summary>
    /// <typeparam name="TKey">Data type of unique keys used on all model classes for this database</typeparam>
    public abstract class SqlDb<TKey> : IDb
    {
        public const string IdentityColumnName = "Id";        

        public string UserName { get; protected set; }

        public int Version { get; protected set; }

        public string ConnectionName { get; protected set; }

        private readonly string _connectionString;

        public SqlDb(Configuration configuration, string connectionName, string userName = null)
        {
            _connectionString = configuration.ConnectionStrings.ConnectionStrings[connectionName].ConnectionString;
            UserName = userName;
            ConnectionName = connectionName;
        }

        public SqlDb(string connection, string userName = null, ConnectionSource connectionSource = ConnectionSource.ConfigFile)
        {
            UserName = userName;

            switch (connectionSource)
            {
                case ConnectionSource.ConfigFile:
                    try
                    {                        
                        _connectionString = ConfigurationManager.ConnectionStrings[connection].ConnectionString;
                        ConnectionName = connection;
                    }
                    catch (NullReferenceException)
                    {
                        string fileName = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;                        
                        string allConnections = AllConnectionNames();
                        throw new NullReferenceException($"Connection string named '{connection}' was not found in {fileName}. These connection names are defined: {allConnections}");                        
                    }
                    break;

                case ConnectionSource.Literal:
                    _connectionString = connection;
                    break;
            }

            if (_connectionString.StartsWith("@"))
            {
                string name = _connectionString.Substring(1);
                _connectionString = ConnectionStringReference.Resolve(name);
            }
        }

        private string FindConnectionString(string location, string connection)
        {
            throw new NotImplementedException();
        }

        private string AllConnectionNames()
        {
            var connections = ConfigurationManager.ConnectionStrings;
            List<string> results = new List<string>();
            foreach (ConnectionStringSettings css in connections)
            {
                results.Add(css.Name);
            }
            return string.Join(", ", results);
        }

        protected string ConnectionString
        {
            get { return _connectionString; }
        }        

        public abstract IDbConnection GetConnection();

        private Dictionary<string, string> _insertCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _updateCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _findCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _deleteCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _copyCommands = new Dictionary<string, string>();

        public bool ExistsWhere<TRecord>(IDbConnection connection, string criteria, object parameters) where TRecord : Record<TKey>
        {
            TRecord record = FindWhere<TRecord>(connection, criteria, parameters);
            return (record != null);
        }

        public TRecord Find<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            var row = ExecuteFind<TRecord>(connection, id);
            return FindInner<TRecord>(connection, row);
        }        

        public TRecord FindWhere<TRecord>(IDbConnection connection, string critieria, object parameters) where TRecord : Record<TKey>
        {
            var row = ExecuteFindWhere<TRecord>(connection, critieria, parameters);
            return FindInner(connection, row);
        }

        public void Delete<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            string message;
            if (record.AllowDelete(connection, UserName, out message))
            {
                ExecuteDelete<TRecord>(connection, record.Id);
                record.AfterDelete(connection);
            }
            else
            {
                throw new PermissionDeniedException(message);
            }
        }

        public void Delete<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            TRecord record = Find<TRecord>(connection, id);
            if (record != null) Delete(connection, record);
        }
        
        public void DeleteWhere<TRecord>(IDbConnection connection, string criteria, object parameters) where TRecord : Record<TKey>
        {
            TRecord record = FindWhere<TRecord>(connection, criteria, parameters);
            if (record != null) Delete<TRecord>(connection, record.Id);
        }

        public void Save<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            SaveAction action;
            Save(connection, record, out action);
        }

        public void Save<TRecord>(IDbConnection connection, TRecord record, out SaveAction action) where TRecord : Record<TKey>
        {
            action = (record.IsNew()) ? SaveAction.Insert : SaveAction.Update;
            SaveInner(connection, record, action, (r) =>
            {
                if (r.IsNew())
                {
                    r.Id = ExecuteInsert(connection, r);
                }
                else
                {
                    ExecuteUpdate(connection, r);
                }
            });
        }

        private void SaveInner<TRecord>(IDbConnection connection, TRecord record, SaveAction action, Action<TRecord> saveAction) where TRecord : Record<TKey>
        {
            record.BeforeSave(connection, UserName, action);

            string message;
            if (record.IsValid(connection, action, out message))
            {
                if (record.AllowSave(connection, UserName, out message))
                {
                    string ignoreProps;
                    if (action == SaveAction.Update && HasChangeTracking<TRecord>(out ignoreProps)) CaptureChanges(connection, record, ignoreProps);

                    saveAction.Invoke(record);

                    record.AfterSave(connection, action);
                }
                else
                {
                    throw new PermissionDeniedException(message);
                }
            }
            else
            {
                throw new ValidationException(message);
            }
        }        

        /// <summary>
        /// Inserts or updates the given records from an open connection. Does not set the record Id property unless the batchSize argument is 1 or less.
        /// </summary>
        public async Task SaveMultipleAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>
        {
            var exc = await SaveMultipleInnerAsync(connection, records, batchSize, cancellationToken, progress);
            if (exc != null) throw new SaveException(exc.Message, exc.CommandText, exc.Record);
        }

        /// <summary>
        /// Inserts or updates the given records. Does not set the record Id property unless the batchSize argument to 1 or less.
        /// </summary>
        public async Task SaveMultipleAsync<TRecord>(IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>
        {
            SaveException exc = null;
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                exc = await SaveMultipleInnerAsync(cn, records, batchSize, cancellationToken, progress);
            }
            if (exc != null) throw new SaveException(exc.Message, exc.CommandText, exc.Record);
        }

        private async Task<SaveException> SaveMultipleInnerAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>
        {
            if (batchSize > 1)
            {
                return await SaveInBatches(connection, records, batchSize, progress, cancellationToken);
            }
            else
            {
                return await SaveEachInnerAsync(connection, records, progress, cancellationToken);
            }            
        }

        private async Task<SaveException> SaveEachInnerAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, IProgress<int> progress, CancellationToken cancellationToken) where TRecord : Record<TKey>
        {
            SaveException exc = null;

            await Task.Run(() =>
            {
                int percentDone = 0;
                int count = 0;
                int totalCount = records.Count();
                foreach (var record in records)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        Save(connection, record);
                    }
                    catch (SaveException excInner)
                    {
                        exc = excInner;
                        break;
                    }
                    
                    count++;
                    percentDone = Convert.ToInt32(Convert.ToDouble(count) / Convert.ToDouble(totalCount) * 100);
                    progress?.Report(percentDone);
                }
            });

            return exc;
        }

        private async Task<SaveException> SaveInBatches<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize, IProgress<int> progress, CancellationToken cancellationToken) where TRecord : Record<TKey>
        {
            Func<TRecord, bool> insertPredicate = (r) => { return r.IsNew(); };
            Func<TRecord, bool> updatePredicate = (r) => { return !r.IsNew(); };

            var operations = new[]
            {
                new { Action = SaveAction.Insert, Predicate = insertPredicate, Command = GetInsertStatement<TRecord>() },
                new { Action = SaveAction.Update, Predicate = updatePredicate, Command = GetUpdateStatement<TRecord>() }
            };

            SaveException exc = null;

            await Task.Run(() =>
            {
                // thanks to accepted answer at http://stackoverflow.com/questions/10689779/bulk-inserts-taking-longer-than-expected-using-dapper
                int batch = 0;
                do
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    using (IDbTransaction trans = connection.BeginTransaction())
                    {
                        var subset = records.Skip(batch * batchSize).Take(batchSize);
                        if (!subset.Any()) break;

                        foreach (var op in operations)
                        {
                            var subsetRecords = subset.Where(r => op.Predicate.Invoke(r));

                            string errorMessage = null;
                            var invalidRecord = subset.FirstOrDefault(item => !item.IsValid(connection, op.Action, out errorMessage));
                            if (invalidRecord != null)
                            {
                                exc = new SaveException(errorMessage, op.Command, invalidRecord);
                                break;
                            }

                            connection.Execute(op.Command, subsetRecords, trans);
                        }

                        trans.Commit();
                    }
                    batch++;
                    progress?.Report(batch * batchSize);
                } while (true);
            });

            return exc;
        }

        public void Update<TRecord>(IDbConnection connection, TRecord record, params Expression<Func<TRecord, object>>[] setColumns) where TRecord : Record<TKey>
        {
            Type modelType = typeof(TRecord);
            IdentityColumnAttribute idAttr;
            string identityCol = (modelType.HasAttribute(out idAttr)) ? idAttr.ColumnName : IdentityColumnName;
            bool useAltIdentity = (!identityCol.Equals(IdentityColumnName));
            PropertyInfo piIdentity = null;
            if (useAltIdentity) piIdentity = modelType.GetProperty(identityCol);

            DynamicParameters dp = new DynamicParameters();
            dp.Add(identityCol, (!useAltIdentity) ? record.Id : piIdentity.GetValue(record));

            List<string> columnNames = new List<string>();
            string setClause = string.Join(", ", setColumns.Select(expr =>
            {
                string propName = PropertyNameFromLambda(expr);
                columnNames.Add(propName);
                PropertyInfo pi = typeof(TRecord).GetProperty(propName);
                dp.Add(propName, expr.Compile().Invoke(record));
                return $"[{pi.SqlColumnName()}]=@{propName}";
            }).Concat(
                modelType.GetProperties().Where(pi => 
                    pi.HasAttribute<ColumnAccessAttribute>(a => a.Access == Access.UpdateOnly))
                        .Select(pi =>
                        {
                            if (columnNames.Contains(pi.SqlColumnName())) throw new InvalidOperationException($"Can't set column {pi.SqlColumnName()} with the Update method because it has a ColumnAccess(UpdateOnly) attribute.");
                            return $"[{pi.SqlColumnName()}]=@{pi.SqlColumnName()}";
                        })));

            string cmd = $"UPDATE {GetTableName<TRecord>()} SET {setClause} WHERE [{identityCol}]=@{identityCol}";

            SaveInner(connection, record, SaveAction.Update, (r) =>
            {
                connection.Execute(cmd, r);
            });            
        }        

        public TRecord Copy<TRecord>(TKey sourceId, object setProperties, IEnumerable<string> omitColumns = null) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return Copy<TRecord>(cn, sourceId, setProperties, omitColumns);
            }
        }

        public TRecord Copy<TRecord>(IDbConnection connection, TKey sourceId, object setProperties, IEnumerable<string> omitColumns = null) where TRecord : Record<TKey>
        {
            TKey newId = ExecuteCopy<TRecord>(connection, sourceId, setProperties, omitColumns);
            return ExecuteFind<TRecord>(connection, newId);
        }

        protected string PropertyNameFromLambda(Expression expression)
        {
            // thanks to http://odetocode.com/blogs/scott/archive/2012/11/26/why-all-the-lambdas.aspx
            // thanks to http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

            LambdaExpression le = expression as LambdaExpression;
            if (le == null) throw new ArgumentException("expression");

            MemberExpression me = null;
            if (le.Body.NodeType == ExpressionType.Convert)
            {
                me = ((UnaryExpression)le.Body).Operand as MemberExpression;
            }
            else if (le.Body.NodeType == ExpressionType.MemberAccess)
            {
                me = le.Body as MemberExpression;
            }

            if (me == null) throw new ArgumentException("expression");

            return me.Member.Name;
        }

        private string GetTableName<TRecord>() where TRecord : Record<TKey>
        {
            Type modelType = typeof(TRecord);            
            string result = modelType.Name;

            TableAttribute tblAttr;
            if (modelType.HasAttribute(out tblAttr))
            {
                result = tblAttr.Name;
                if (!string.IsNullOrEmpty(tblAttr.Schema))
                {
                    result = tblAttr.Schema + "." + tblAttr.Name;
                }
            }

            return ApplyDelimiter(result);
        }

        private string GetFindStatement<TRecord>() where TRecord : Record<TKey>
        {
            return GetFindStatementBase<TRecord>() + $" WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        private string GetFindStatementBase<TRecord>() where TRecord : Record<TKey>
        {
            return
                $@"SELECT {ApplyDelimiter(IdentityColumnName)},
                    {string.Join(", ", GetColumnNames<TRecord>().Select(name => ApplyDelimiter(name)))} 
                FROM 
                    {GetTableName<TRecord>()}";
        }

        private string GetInsertStatement<TRecord>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord>(pi => pi.HasColumnAccess(Access.InsertOnly));

            return 
                $@"INSERT INTO {GetTableName<TRecord>()} (
                    {string.Join(", ", columns.Select(s => ApplyDelimiter(s)))}
                ) OUTPUT [inserted].[{typeof(TRecord).IdentityColumnName()}] VALUES (
                    {string.Join(", ", columns.Select(s => $"@{s}"))}
                )";
        }

        private string GetUpdateStatement<TRecord>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord>(pi => pi.HasColumnAccess(Access.UpdateOnly));

            return 
                $@"UPDATE {GetTableName<TRecord>()} SET
                    {string.Join(", ", columns.Select(s => $"{ApplyDelimiter(s)} = @{s}"))}
                WHERE 
                    [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        private string GetDeleteStatement<TRecord>() where TRecord : Record<TKey>
        {
            return $"DELETE {GetTableName<TRecord>()} WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        private string GetCopyStatement<TRecord>(object parameters, IEnumerable<string> omitColumns) where TRecord : Record<TKey>
        {
            var paramColumns = parameters.GetType().GetProperties().Select(pi => pi.Name);

            var columns = GetColumnNames<TRecord>(pi => 
                    !pi.HasAttribute<CalculatedAttribute>()) // can't insert into calculated columns
                .Where(s => 
                    !s.Equals(typeof(TRecord).IdentityColumnName()) && // can't insert into identity column
                    (!omitColumns?.Select(omitCol => omitCol.ToLower()).Contains(s.ToLower()) ?? false) &&
                    !paramColumns.Select(paramCol => paramCol.ToLower()).Contains(s.ToLower())) // don't insert into param columns because we're providing new values    
                .Select(colName => ApplyDelimiter(colName));            

            return
                $@"INSERT INTO {GetTableName<TRecord>()} (
                    {string.Join(", ", columns.Concat(paramColumns.Select(col => ApplyDelimiter(col))))}
                ) OUTPUT 
                    [inserted].[{typeof(TRecord).IdentityColumnName()}] 
                SELECT 
                    {string.Join(", ", columns.Concat(paramColumns.Select(col => $"@{col}")))}
                FROM 
                    {GetTableName<TRecord>()} 
                WHERE 
                    [{typeof(TRecord).IdentityColumnName()}]=@id";
        }           

        private IEnumerable<PropertyInfo> GetEditableColumns<TRecord>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {            
            return typeof(TRecord).GetProperties().Where(pi =>
                !pi.Name.Equals(IdentityColumnName) && 
                !pi.HasAttribute<CalculatedAttribute>() &&
                (!pi.HasAttribute<ColumnAccessAttribute>() || (predicate?.Invoke(pi) ?? true)));
        }

        private IEnumerable<string> GetColumnNames<TRecord>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {
            return GetEditableColumns<TRecord>(predicate).Select(pi =>
            {
                ColumnAttribute colAttr;
                return (pi.HasAttribute(out colAttr)) ? colAttr.Name : pi.Name;                
            });
        }

        protected abstract string ApplyDelimiter(string name);

        private TRecord FindInner<TRecord>(IDbConnection connection, TRecord row) where TRecord : Record<TKey>
        {
            if (row == null) return null;

            string message;
            if (row.AllowView(connection, UserName, out message))
            {
                return row;
            }
            else
            {
                throw new PermissionDeniedException(message);
            }
        }

        private TKey ExecuteInsert<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_insertCommands, () => GetInsertStatement<TRecord>());
            try
            {
                return connection.QuerySingle<TKey>(cmd, record);
            }
            catch (Exception exc)
            {
                throw new SaveException(exc.Message, cmd, record);
            }
        }

        private void ExecuteUpdate<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_updateCommands, () => GetUpdateStatement<TRecord>());
            try
            {
                connection.Execute(cmd, record);
            }
            catch (Exception exc)
            {
                throw new SaveException(exc.Message, cmd, record);
            }
        }

        private TRecord ExecuteFind<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_findCommands, () => GetFindStatement<TRecord>());
            return connection.QueryFirstOrDefault<TRecord>(cmd, new { id = id });
        }

        private TRecord ExecuteFindWhere<TRecord>(IDbConnection connection, string criteria, object parameters) where TRecord : Record<TKey>
        {
            string cmd = GetFindStatementBase<TRecord>() + $" WHERE {criteria}";
            return connection.QuerySingleOrDefault<TRecord>(cmd, parameters);                       
        }

        private void ExecuteDelete<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_deleteCommands, () => GetDeleteStatement<TRecord>());
            connection.Execute(cmd, new { id = id });
        } 
        
        private TKey ExecuteCopy<TRecord>(IDbConnection connection, TKey id, object parameters, IEnumerable<string> omitColumns) where TRecord: Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_copyCommands, () => GetCopyStatement<TRecord>(parameters, omitColumns));
            DynamicParameters dp = new DynamicParameters(parameters);
            dp.Add(typeof(TRecord).IdentityColumnName(), id);
            return connection.QuerySingle<TKey>(cmd, dp);
        }

        private string GetCommand<TRecord>(Dictionary<string, string> dictionary, Func<string> commandBuilder)
        {
            string modelTypeName = typeof(TRecord).Name;
            if (!dictionary.ContainsKey(modelTypeName)) dictionary.Add(modelTypeName, commandBuilder.Invoke());
            return dictionary[modelTypeName];
        }

        public IEnumerable<PropertyChange> GetChanges<TRecord>(IDbConnection connection, TRecord record, string ignoreProps = null) where TRecord : Record<TKey>
        {
            if (record.IsNew()) return null;

            string[] ignorePropsArray = (ignoreProps ?? string.Empty).Split(',', ';').Select(s => s.Trim()).ToArray();

            TRecord savedRecord = Find<TRecord>(connection, record.Id);
            return typeof(TRecord).GetProperties().Where(pi => pi.HasColumnAccess(Access.UpdateOnly) && !ignorePropsArray.Contains(pi.Name)).Select(pi =>
            {
                return new PropertyChange()
                {
                    PropertyName = pi.Name,
                    OldValue = OnGetChangesPropertyValue(pi, savedRecord, connection),
                    NewValue = OnGetChangesPropertyValue(pi, record, connection)
                };
            }).Where(vc => vc.IsChanged());
        }

        protected virtual object OnGetChangesPropertyValue(PropertyInfo propertyInfo, object record, IDbConnection connection)
        {
            return propertyInfo.GetValue(record);
        }

        public void CaptureChanges<TRecord>(IDbConnection connection, TRecord record, string ignoreProps = null) where TRecord : Record<TKey>
        {
            var changes = GetChanges(connection, record, ignoreProps);
            if (changes?.Any() ?? false) OnCaptureChanges<TRecord>(connection, record.Id, changes);
        }

        protected abstract void OnCaptureChanges<TRecord>(IDbConnection connection, TKey id, IEnumerable<PropertyChange> changes) where TRecord : Record<TKey>;

        public abstract IEnumerable<ChangeHistory<TKey>> QueryChangeHistory<TRecord>(IDbConnection connection, TKey id, int timeZoneOffset = 0) where TRecord : Record<TKey>;

        private bool HasChangeTracking<TRecord>(out string ignoreProperties) where TRecord : Record<TKey>
        {
            TrackChangesAttribute attr;
            if (typeof(TRecord).HasAttribute(out attr))
            {
                ignoreProperties = attr.IgnoreProperties;
                return true;
            }
            ignoreProperties = null;
            return false;

        }
    }
}
