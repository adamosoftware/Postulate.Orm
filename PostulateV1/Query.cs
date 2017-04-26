using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Postulate
{
    public class Query<TResult>
    {
        private readonly string _sql;
        private readonly Func<IDbConnection> _connectionGetter;

        public Query(string sql, Func<IDbConnection> connectionGetter)
        {
            _sql = sql;
            _connectionGetter = connectionGetter;
        }

        public string Sql { get { return _sql; } }

        public int CommandTimeout { get; set; } = 30;

        public CommandType CommandType { get; set; } = CommandType.Text;

        public IEnumerable<TResult> Execute()
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return Execute(cn);
            }
        }

        public IEnumerable<TResult> Execute(IDbConnection connection)
        {
            return connection.Query<TResult>(_sql, this, commandType: CommandType);
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync()
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return await ExecuteAsync(cn);
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(IDbConnection connection)
        {
            return await connection.QueryAsync<TResult>(_sql, this, commandType: CommandType);
        }

        public TResult ExecuteSingle()
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return ExecuteSingle(cn);
            }
        }

        public TResult ExecuteSingle(IDbConnection connection)
        {
            return connection.QuerySingleOrDefault<TResult>(_sql, this, commandType: CommandType);
        }

        public async Task<TResult> ExecuteSingleAsync()
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return await ExecuteSingleAsync(cn);
            }
        }

        public async Task<TResult> ExecuteSingleAsync(IDbConnection connection)
        {
            return await connection.QuerySingleOrDefaultAsync<TResult>(_sql, this, commandType: CommandType);
        }
    }
}
