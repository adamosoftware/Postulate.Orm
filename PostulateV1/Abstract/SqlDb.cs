using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
    public abstract partial class SqlDb<TKey> : IDb
    {
        public const string IdentityColumnName = "Id";

        public string UserName { get; set; }

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

        public abstract IDbTransaction GetTransaction(IDbConnection connection);

        private Dictionary<string, string> _insertCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _updateCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _findCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _deleteCommands = new Dictionary<string, string>();
        private Dictionary<string, string> _copyCommands = new Dictionary<string, string>();

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
                row.BeforeView(connection);
                return row;
            }
            else
            {
                throw new PermissionDeniedException(message);
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

        private string GetCommand<TRecord>(Dictionary<string, string> dictionary, Func<string> commandBuilder)
        {
            string modelTypeName = typeof(TRecord).Name;
            if (!dictionary.ContainsKey(modelTypeName)) dictionary.Add(modelTypeName, commandBuilder.Invoke());
            return dictionary[modelTypeName];
        }

        private string ParseWhereClause<TRecord>(Expression<Func<TRecord, bool>> expression)
        {
            // thanks to https://stackoverflow.com/questions/22912649/lambda-to-sql-translation
            throw new NotImplementedException();
        }
    }
}