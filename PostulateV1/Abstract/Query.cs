using Postulate.Orm.Interfaces;
using Postulate.Orm.Models;
using System;
using System.Data;

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
    }
}