using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using Dapper;

namespace Postulate.Orm.SqlCe
{
    public class SqlCeDb : SqlDb<int>
    {
        public SqlCeDb(string connectionString) : base(connectionString, new SqlCeSyntax(), connectionSource: ConnectionSource.Literal)
        {
        }

        public override void CreateIfNotExists(Action<IDbConnection, bool> seedAction = null)
        {
            var engine = new SqlCeEngine(ConnectionString);
            engine.CreateDatabase();
        }

        public override IDbConnection GetConnection()
        {
            return new SqlCeConnection(ConnectionString);
        }

        public override int GetRecordNextVersion<TRecord>(IDbConnection connection, int id)
        {
            throw new NotSupportedException();
        }

        public override int GetRecordNextVersion<TRecord>(int id)
        {
            throw new NotSupportedException();
        }

        public override IDbTransaction GetTransaction(IDbConnection connection)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<ChangeHistory<int>> QueryChangeHistory<TRecord>(IDbConnection connection, int id, int timeZoneOffset = 0)
        {
            throw new NotSupportedException();
        }

        protected override TRecord BeginRestore<TRecord>(IDbConnection connection, int id)
        {
            throw new NotSupportedException();
        }

        protected override void CompleteRestore<TRecord>(IDbConnection connection, int id, IDbTransaction transaction)
        {
            throw new NotSupportedException();
        }

        protected override void OnCaptureChanges<TRecord>(IDbConnection connection, int id, IEnumerable<PropertyChange> changes)
        {
            throw new NotSupportedException();
        }

        protected override void OnCaptureDeletion<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transasction)
        {
            throw new NotSupportedException();
        }

        protected override string GetInsertStatement<TRecord>()
        {
            var columns = GetColumnNames<TRecord>(pi => pi.HasColumnAccess(Access.InsertOnly));

            return
                $@"INSERT INTO {GetTableName<TRecord>()} (
                    {string.Join(", ", columns.Select(col => Syntax.ApplyDelimiter(col.ColumnName)))}
                ) VALUES (
                    {string.Join(", ", columns.Select(s => $"@{s.PropertyName}"))}
                )";
        }

        protected override int ExecuteInsertMethod<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction, string cmd)
        {
            connection.Execute(cmd, record, transaction);
            return connection.QuerySingle<int>("SELECT @@IDENTITY");
        }
    }
}