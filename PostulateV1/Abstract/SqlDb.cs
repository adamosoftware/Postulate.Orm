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

namespace Postulate.Abstract
{
    public abstract class SqlDb
    {
        public const string IdentityColumnName = "Id";        

        public string UserName { get; protected set; }

        private readonly string _connectionString;

        public SqlDb(string connectionName)
        {
            try
            {
                _connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
            }
            catch (NullReferenceException)
            {
                string fileName = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                string allConnections = AllConnectionNames();
                throw new NullReferenceException($"Connection string named {connectionName} was not found in {fileName}. These connection names are defined: {allConnections}");
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

        public bool Exists<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            TRecord record = Find<TRecord, TKey>(connection, id);
            return (record != null);
        }

        public bool ExistsWhere<TRecord, TKey>(IDbConnection connection, string criteria) where TRecord : Record<TKey>
        {
            TRecord record = FindWhere<TRecord, TKey>(connection, criteria);
            return (record != null);
        }

        public TRecord Find<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            var row = ExecuteFind<TRecord, TKey>(connection, id);
            return FindInner<TRecord, TKey>(connection, row);
        }        

        public TRecord FindWhere<TRecord, TKey>(IDbConnection connection, string critieria) where TRecord : Record<TKey>
        {
            var row = ExecuteFindWhere<TRecord, TKey>(connection, critieria);
            return FindInner<TRecord, TKey>(connection, row);
        }

        public void Delete<TRecord, TKey>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            string message;
            if (record.AllowDelete(connection, UserName, out message))
            {
                ExecuteDelete<TRecord, TKey>(connection, record.Id);
                record.AfterDelete(connection);
            }
            else
            {
                throw new PermissionDeniedException(message);
            }
        }

        public void Delete<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            TRecord record = Find<TRecord, TKey>(connection, id);
            Delete<TRecord, TKey>(connection, record);
        }
        
        public void Save<TRecord, TKey>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            SaveAction action;
            Save<TRecord, TKey>(connection, record, out action);
        }

        public void Save<TRecord, TKey>(IDbConnection connection, TRecord record, out SaveAction action) where TRecord : Record<TKey>
        {
            action = SaveAction.NotSet;
            string message;
            if (record.IsValid(connection, out message))
            {
                if (record.AllowSave(connection, UserName, out message))
                {
                    action = (record.IsNewRow()) ? SaveAction.Insert : SaveAction.Update;

                    record.BeforeSave(connection, UserName, action);

                    if (record.IsNewRow())
                    {
                        record.Id = ExecuteInsert<TRecord, TKey>(connection, record);
                    }
                    else
                    {
                        ExecuteUpdate<TRecord, TKey>(connection, record);
                    }

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

        protected string GetTableName<TRecord, TKey>() where TRecord : Record<TKey>
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

            return DelimitName(result);
        }
            
        protected string GetFindStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            return GetFindStatementBase<TRecord, TKey>() + $" WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        private string GetFindStatementBase<TRecord, TKey>() where TRecord : Record<TKey>
        {
            return
                $@"SELECT 
                    {string.Join(", ", GetColumnNames<TRecord, TKey>().Select(name => DelimitName(name)))} 
                FROM 
                    {GetTableName<TRecord, TKey>()}";
        }

        protected string GetInsertStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord, TKey>(pi => pi.HasColumnAccess(Access.InsertOnly));

            return 
                $@"INSERT INTO {GetTableName<TRecord, TKey>()} (
                    {string.Join(", ", columns.Select(s => DelimitName(s)))}
                ) OUTPUT [inserted].[{typeof(TRecord).IdentityColumnName()}] VALUES (
                    {string.Join(", ", columns.Select(s => $"@{s}"))}
                )";
        }

        protected string GetUpdateStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord, TKey>(pi => pi.HasColumnAccess(Access.UpdateOnly));

            return 
                $@"UPDATE {GetTableName<TRecord, TKey>()} SET
                    {string.Join(", ", columns.Select(s => $"{DelimitName(s)} = @{s}"))}
                WHERE 
                    [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        protected string GetDeleteStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            return $"DELETE {GetTableName<TRecord, TKey>()} WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        protected IEnumerable<PropertyInfo> GetEditableColumns<TRecord, TKey>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {            
            return typeof(TRecord).GetProperties().Where(pi =>
                !pi.Name.Equals(IdentityColumnName) && 
                !pi.HasAttribute<CalculatedAttribute>() &&
                (!pi.HasAttribute<ColumnAccessAttribute>() || (predicate?.Invoke(pi) ?? true)));
        }

        protected IEnumerable<string> GetColumnNames<TRecord, TKey>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {
            return GetEditableColumns<TRecord, TKey>(predicate).Select(pi =>
            {
                ColumnAttribute colAttr;
                return (pi.HasAttribute(out colAttr)) ? colAttr.Name : pi.Name;                
            });
        }        

        protected abstract string DelimitName(string name);

        private TRecord FindInner<TRecord, TKey>(IDbConnection connection, TRecord row) where TRecord : Record<TKey>
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

        private TKey ExecuteInsert<TRecord, TKey>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_insertCommands, () => GetInsertStatement<TRecord, TKey>());
            try
            {
                return connection.QuerySingle<TKey>(cmd, record);
            }
            catch (Exception exc)
            {
                throw new SaveException(exc.Message, cmd, record);
            }
        }

        private void ExecuteUpdate<TRecord, TKey>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_updateCommands, () => GetUpdateStatement<TRecord, TKey>());
            try
            {
                connection.Execute(cmd, record);
            }
            catch (Exception exc)
            {
                throw new SaveException(exc.Message, cmd, record);
            }
        }

        private TRecord ExecuteFind<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_findCommands, () => GetFindStatement<TRecord, TKey>());
            return connection.QueryFirstOrDefault<TRecord>(cmd, new { id = id });
        }

        private TRecord ExecuteFindWhere<TRecord, TKey>(IDbConnection connection, string criteria) where TRecord : Record<TKey>
        {
            string cmd = GetFindStatementBase<TRecord, TKey>() + $" WHERE {criteria}";
            return connection.QuerySingleOrDefault<TRecord>(cmd);
        }

        private void ExecuteDelete<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_deleteCommands, () => GetDeleteStatement<TRecord, TKey>());
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
