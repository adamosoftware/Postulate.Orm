﻿using Postulate.Orm.Attributes;
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
        protected readonly SqlSyntax _syntax;

        public const string IdentityColumnName = "Id";

        public string UserName { get; set; }

        public int Version { get; protected set; }

        public string ConnectionName { get; protected set; }

        private readonly string _connectionString;

        public SqlDb(Configuration configuration, string connectionName, SqlSyntax syntax, string userName = null)
        {
            _syntax = syntax;
            _connectionString = configuration.ConnectionStrings.ConnectionStrings[connectionName].ConnectionString;
            UserName = userName;
            ConnectionName = connectionName;
        }

        public SqlDb(string connection, SqlSyntax syntax, string userName = null, ConnectionSource connectionSource = ConnectionSource.ConfigFile)
        {
            _syntax = syntax;

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

        protected virtual string GetTableName<TRecord>() where TRecord : Record<TKey>
        {
            Type modelType = typeof(TRecord);
            string schema;
            string tableName;
            CreateTable.ParseNameAndSchema(modelType, out schema, out tableName);
            string result = schema + "." + tableName;
            return _syntax.ApplyDelimiter(result);
        }

        protected virtual string GetFindStatement<TRecord>() where TRecord : Record<TKey>, new()
        {
            string whereClause = $" WHERE {_syntax.ApplyDelimiter(typeof(TRecord).IdentityColumnName())}=@id";

            string customWhere = (new TRecord()).CustomFindWhereClause();
            if (!string.IsNullOrEmpty(customWhere)) whereClause = " WHERE " + customWhere;

            return GetFindStatementBase<TRecord>() + whereClause;
        }

        protected virtual string GetFindStatementBase<TRecord>() where TRecord : Record<TKey>, new()
        {
            string customCmd = (new TRecord()).CustomFindCommandText();
            if (!string.IsNullOrEmpty(customCmd)) return customCmd;

            return
                $@"SELECT {_syntax.ApplyDelimiter(typeof(TRecord).IdentityColumnName())},
                    {string.Join(", ", GetColumnNames<TRecord>().Select(name => _syntax.ApplyDelimiter(name)).Concat(GetCalculatedColumnNames<TRecord>()))}
                FROM
                    {GetTableName<TRecord>()}";
        }

        private IEnumerable<string> GetCalculatedColumnNames<TRecord>() where TRecord : Record<TKey>
        {
            return typeof(TRecord).GetProperties().Where(pi =>
                pi.HasAttribute<CalculatedAttribute>() &&
                pi.IsSupportedType(_syntax)).Select(pi => _syntax.ApplyDelimiter(pi.SqlColumnName()));
        }

        protected virtual string GetInsertStatement<TRecord>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord>(pi => pi.HasColumnAccess(Access.InsertOnly));

            return
                $@"INSERT INTO {GetTableName<TRecord>()} (
                    {string.Join(", ", columns.Select(s => _syntax.ApplyDelimiter(s)))}
                ) OUTPUT [inserted].[{typeof(TRecord).IdentityColumnName()}] VALUES (
                    {string.Join(", ", columns.Select(s => $"@{s}"))}
                )";
        }

        protected virtual string GetUpdateStatement<TRecord>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord>(pi => pi.HasColumnAccess(Access.UpdateOnly));

            return
                $@"UPDATE {GetTableName<TRecord>()} SET
                    {string.Join(", ", columns.Select(s => $"{_syntax.ApplyDelimiter(s)} = @{s}"))}
                WHERE
                    {_syntax.ApplyDelimiter(typeof(TRecord).IdentityColumnName())}=@id";
        }

        protected virtual string GetDeleteStatement<TRecord>() where TRecord : Record<TKey>
        {
            return $"DELETE {GetTableName<TRecord>()} WHERE {_syntax.ApplyDelimiter(typeof(TRecord).IdentityColumnName())}=@id";
        }

        protected IEnumerable<PropertyInfo> GetEditableColumns<TRecord>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {
            return typeof(TRecord).GetProperties().Where(pi =>
                !pi.Name.Equals(IdentityColumnName) &&
                !pi.Name.Equals(typeof(TRecord).IdentityColumnName()) &&
                !pi.HasAttribute<CalculatedAttribute>() &&
                !pi.HasAttribute<NotMappedAttribute>() &&
                pi.IsSupportedType(_syntax) &&
                (predicate?.Invoke(pi) ?? true));
        }

        protected IEnumerable<string> GetColumnNames<TRecord>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {
            return GetEditableColumns<TRecord>(predicate).Select(pi =>
            {
                ColumnAttribute colAttr;
                return (pi.HasAttribute(out colAttr) && !string.IsNullOrEmpty(colAttr.Name)) ? colAttr.Name : pi.Name;
            });
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