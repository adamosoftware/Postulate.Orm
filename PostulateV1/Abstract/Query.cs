using Dapper;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Postulate.Orm.Abstract
{
    public abstract class Query<TResult>
    {
        private readonly string _sql;
        private string _resolvedSql;
        private readonly IDb _db;

        public Action<IDbConnection, QueryTrace> TraceCallback { get; set; }

        public Query(string sql, IDb db)
        {
            _sql = sql;
            _db = db;
        }

        public string Sql { get { return _sql; } }

        public string ResolvedSql { get { return _resolvedSql; } }

        public int CommandTimeout { get; set; } = 30;

        public CommandType CommandType { get; set; } = CommandType.Text;

        public IEnumerable<TResult> Execute(object parameters = null)
        {
            using (var cn = _db.GetConnection())
            {
                cn.Open();
                return ExecuteInner(cn, parameters);
            }
        }

        public IEnumerable<TResult> Execute(IDbConnection connection, object parameters = null)
        {
            return ExecuteInner(connection, parameters);
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(object parameters = null)
        {
            using (var cn = _db.GetConnection())
            {
                cn.Open();
                return await ExecuteInnerAsync(cn, parameters);
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(IDbConnection connection, object parameters = null)
        {
            return await ExecuteInnerAsync(connection, parameters);
        }

        private IEnumerable<TResult> ExecuteInner(IDbConnection connection, object parameters)
        {
            IEnumerable<TResult> results = null;
            Exception queryException = null;
            Stopwatch sw = Stopwatch.StartNew();

            _resolvedSql = ResolveQuery();

            try
            {                
                results = connection.Query<TResult>(_resolvedSql, parameters);
            }
            catch (Exception exc)
            {
                queryException = exc;
            }
            finally
            {
                sw.Stop();
                if (queryException == null) TraceCallback?.Invoke(connection, new QueryTrace(_resolvedSql, parameters, sw.ElapsedMilliseconds));
            }

            if (queryException != null) throw queryException;

            return results;
        }        

        private async Task<IEnumerable<TResult>> ExecuteInnerAsync(IDbConnection connection, object parameters)
        {
            IEnumerable<TResult> results = null;
            Exception queryException = null;
            Stopwatch sw = Stopwatch.StartNew();

            _resolvedSql = ResolveQuery();

            try
            {
                results = await connection.QueryAsync<TResult>(_resolvedSql, parameters);
            }
            catch (Exception exc)
            {
                queryException = exc;
            }
            finally
            {
                sw.Stop();
                if (queryException == null) TraceCallback?.Invoke(connection, new QueryTrace(_resolvedSql, parameters, sw.ElapsedMilliseconds));
            }

            if (queryException != null) throw queryException;

            return results;

        }

        private string ResolveQuery()
        {
            throw new NotImplementedException();
        }
    }
}