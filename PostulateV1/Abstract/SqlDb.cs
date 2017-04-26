using Postulate.Attributes;
using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;
using Postulate.Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using Postulate.Exceptions;
using Dapper;
using System.Configuration;
using System.Linq.Expressions;

namespace Postulate.Abstract
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
    public abstract class SqlDb<TKey>
    {
        public const string IdentityColumnName = "Id";        

        public string UserName { get; protected set; }

        private readonly string _connectionString;

        public SqlDb(string connection, ConnectionSource connectionSource = ConnectionSource.ConfigFile)
        {
            switch (connectionSource)
            {
                case ConnectionSource.ConfigFile:
                    try
                    {
                        _connectionString = ConfigurationManager.ConnectionStrings[connection].ConnectionString;
                    }
                    catch (NullReferenceException)
                    {
                        string fileName = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                        string allConnections = AllConnectionNames();
                        throw new NullReferenceException($"Connection string named {connection} was not found in {fileName}. These connection names are defined: {allConnections}");
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
            Delete<TRecord>(connection, record);
        }
        
        public void Save<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            SaveAction action;
            Save(connection, record, out action);
        }

        public void Save<TRecord>(IDbConnection connection, TRecord record, out SaveAction action) where TRecord : Record<TKey>
        {
            action = (record.IsNewRow()) ? SaveAction.Insert : SaveAction.Update;
            SaveInner(connection, record, action, (r) =>
            {
                if (r.IsNewRow())
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

            string setClause = string.Join(", ", setColumns.Select(expr =>
            {
                string propName = PropertyNameFromLambda(expr);
                PropertyInfo pi = typeof(TRecord).GetProperty(propName);
                dp.Add(propName, expr.Compile().Invoke(record));
                return $"[{pi.SqlColumnName()}]=@{propName}";
            }));

            string cmd = $"UPDATE {GetTableName<TRecord>()} SET {setClause} WHERE [{identityCol}]=@{identityCol}";

            SaveInner(connection, record, SaveAction.Update, (r) =>
            {
                connection.Execute(cmd, r);
            });            
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
            return GetFindStatementBase<TRecord>() + $" WHERE [{typeof(TRecord).IdentityColumnName<TKey>()}]=@id";
        }

        private string GetFindStatementBase<TRecord>() where TRecord : Record<TKey>
        {
            return
                $@"SELECT 
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
                ) OUTPUT [inserted].[{typeof(TRecord).IdentityColumnName<TKey>()}] VALUES (
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
                    [{typeof(TRecord).IdentityColumnName<TKey>()}]=@id";
        }

        private string GetDeleteStatement<TRecord>() where TRecord : Record<TKey>
        {
            return $"DELETE {GetTableName<TRecord>()} WHERE [{typeof(TRecord).IdentityColumnName<TKey>()}]=@id";
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
            string message;
            if (row.AllowFind(connection, UserName, out message))
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

        private string GetCommand<TRecord>(Dictionary<string, string> dictionary, Func<string> commandBuilder)
        {
            string modelTypeName = typeof(TRecord).Name;
            if (!dictionary.ContainsKey(modelTypeName)) dictionary.Add(modelTypeName, commandBuilder.Invoke());
            return dictionary[modelTypeName];
        }
    }
}
