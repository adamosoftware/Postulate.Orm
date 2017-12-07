using MySql.Data.MySqlClient;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;

namespace Postulate.Orm.MySql
{
    public class MySqlDb<TKey> : SqlDb<TKey>
    {
        public MySqlDb(string connectionName, string userName = null) : base(connectionName, new MySqlSyntax(), userName)
        {
        }

        public MySqlDb(Configuration config, string connectionName, string userName = null) : base(config, connectionName, new MySqlSyntax(), userName)
        {
        }

        public override void CreateIfNotExists(Action<IDbConnection, bool> seedAction = null)
        {
            try
            {
                using (var cn = GetConnection())
                {
                    cn.Open();
                }
            }
            catch (Exception exc)
            {
                throw new NotSupportedException("Currently, the Postulate MySql implementation is not able to create databases on the fly. Please create the database manually and attempt the operation again.", exc);
            }
        }

        public override IDbConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public override int GetRecordNextVersion<TRecord>(IDbConnection connection, TKey id)
        {
            throw new NotSupportedException();
        }

        public override int GetRecordNextVersion<TRecord>(TKey id)
        {
            throw new NotSupportedException();
        }

        public override IDbTransaction GetTransaction(IDbConnection connection)
        {
            return (connection as MySqlConnection).BeginTransaction(IsolationLevel.Serializable);
        }

        public override IEnumerable<ChangeHistory<TKey>> QueryChangeHistory<TRecord>(IDbConnection connection, TKey id, int timeZoneOffset = 0)
        {
            throw new NotSupportedException();
        }

        protected override TRecord BeginRestore<TRecord>(IDbConnection connection, TKey id)
        {
            throw new NotSupportedException();
        }

        protected override void CompleteRestore<TRecord>(IDbConnection connection, TKey id, IDbTransaction transaction)
        {
            throw new NotSupportedException();
        }

        protected override void OnCaptureChanges<TRecord>(IDbConnection connection, TKey id, IEnumerable<PropertyChange> changes)
        {
            throw new NotSupportedException();
        }

        protected override void OnCaptureDeletion<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transasction)
        {
            throw new NotSupportedException();
        }

        protected override string GetTableName<TRecord>()
        {
            Type modelType = typeof(TRecord);
            var obj = TableInfo.FromModelType(typeof(TRecord));
            return Syntax.ApplyDelimiter(obj.Name);
        }

        protected override string GetInsertStatement<TRecord>()
        {
            var columns = GetColumnNames<TRecord>(pi => pi.HasColumnAccess(Access.InsertOnly));

            return
                $@"INSERT INTO {GetTableName<TRecord>()} (
                    {string.Join(", ", columns.Select(col => Syntax.ApplyDelimiter(col.ColumnName)))}
                ) VALUES (
                    {string.Join(", ", columns.Select(col => $"@{col.PropertyName}"))}
                ); SELECT LAST_INSERT_ID()";
        }

        protected override string GetDeleteStatement<TRecord>()
        {
            return $"DELETE FROM {GetTableName<TRecord>()} WHERE {Syntax.ApplyDelimiter(typeof(TRecord).IdentityColumnName())}=@id";
        }
    }
}