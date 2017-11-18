using Dapper;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Models;
using Postulate.Orm.Attributes;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Postulate.Orm.Extensions;


namespace Postulate.Orm.Abstract
{
    public abstract class Query<TResult>
    {
        private readonly string _sql;      
        private readonly IDb _db;
        private string _resolvedSql;

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
            
            _resolvedSql = ResolveWhereClause();

            Stopwatch sw = Stopwatch.StartNew();
            results = connection.Query<TResult>(_resolvedSql, parameters);
            sw.Stop();

            TraceCallback?.Invoke(connection, new QueryTrace(_resolvedSql, parameters, sw.ElapsedMilliseconds));

            return results;
        }

        private async Task<IEnumerable<TResult>> ExecuteInnerAsync(IDbConnection connection, object parameters)
        {
            IEnumerable<TResult> results = null;            

            _resolvedSql = ResolveWhereClause();

            Stopwatch sw = Stopwatch.StartNew();
            results = await connection.QueryAsync<TResult>(_resolvedSql, parameters);
            sw.Stop();

            TraceCallback?.Invoke(connection, new QueryTrace(_resolvedSql, parameters, sw.ElapsedMilliseconds));

            return results;
        }

        private string ResolveWhereClause()
        {
            string result = _sql;

            Dictionary<string, string> whereBuilder = new Dictionary<string, string>()
            {
                { InternalStringExtensions.WhereToken, "WHERE" }, // query has no where clause, so it needs the word WHERE inserted
                { InternalStringExtensions.AndWhereToken, "AND" } // query already contains a WHERE clause, we're just adding to it
            };
            string token;
            if (result.ContainsAny(new string[] { InternalStringExtensions.WhereToken, InternalStringExtensions.AndWhereToken }, out token))
            {
                bool anyCriteria = false;
                List<string> terms = new List<string>();
                var baseProps = GetType().BaseType.GetProperties().Select(pi => pi.Name);
                var builtInParams = _sql.GetParameterNames(true).Select(p => p.ToLower());
                foreach (var pi in GetType().GetProperties().Where(pi => !baseProps.Contains(pi.Name) && !builtInParams.Contains(pi.Name.ToLower())))
                {
                    object value = pi.GetValue(this);
                    if (value != null)
                    {
                        WhereAttribute whereAttr = pi.GetAttribute<WhereAttribute>();
                        string expression = (whereAttr != null) ? whereAttr.Expression : $"{_db.Syntax.ApplyDelimiter(pi.Name)}=@{pi.Name}";
                        terms.Add(expression);
                        anyCriteria = true;
                    }
                }
                result = result.Replace(token, (anyCriteria) ? $"{whereBuilder[token]} {string.Join(" AND ", terms)}" : string.Empty);
            }

            return result;
        }
    }
}