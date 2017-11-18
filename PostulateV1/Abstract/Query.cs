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

        /// <summary>
        /// Lets you specify where in the application a query is called so that trace information is potentially more useful
        /// </summary>
        public string TraceContext { get; set; }

        public IEnumerable<TResult> Execute()
        {
            using (var cn = _db.GetConnection())
            {
                cn.Open();
                return ExecuteInner(cn);
            }
        }

        public IEnumerable<TResult> Execute(IDbConnection connection)
        {
            return ExecuteInner(connection);
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync()
        {
            using (var cn = _db.GetConnection())
            {
                cn.Open();
                return await ExecuteInnerAsync(cn);
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(IDbConnection connection)
        {
            return await ExecuteInnerAsync(connection);
        }

        private IEnumerable<TResult> ExecuteInner(IDbConnection connection)
        {
            IEnumerable<TResult> results = null;

            List<QueryTrace.Parameter> parameters;
            _resolvedSql = ResolveQuery(_sql, this, out parameters);

            Stopwatch sw = Stopwatch.StartNew();
            results = connection.Query<TResult>(_resolvedSql, this);
            sw.Stop();

            TraceCallback?.Invoke(connection, new QueryTrace(_resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext));

            return results;
        }

        private async Task<IEnumerable<TResult>> ExecuteInnerAsync(IDbConnection connection)
        {
            IEnumerable<TResult> results = null;

            List<QueryTrace.Parameter> parameters;
            _resolvedSql = ResolveQuery(_sql, this, out parameters);

            Stopwatch sw = Stopwatch.StartNew();
            results = await connection.QueryAsync<TResult>(_resolvedSql, this);
            sw.Stop();

            TraceCallback?.Invoke(connection, new QueryTrace(_resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext));

            return results;
        }

        private static string ResolveQuery(string sql, Query<TResult> query, out List<QueryTrace.Parameter> parameters)
        {
            string result = sql;
            List<string> terms = new List<string>();
            parameters = new List<QueryTrace.Parameter>();

            Dictionary<string, string> whereBuilder = new Dictionary<string, string>()
            {
                { InternalStringExtensions.WhereToken, "WHERE" }, // query has no where clause, so it needs the word WHERE inserted
                { InternalStringExtensions.AndWhereToken, "AND" } // query already contains a WHERE clause, we're just adding to it
            };
            string token;
            if (result.ContainsAny(new string[] { InternalStringExtensions.WhereToken, InternalStringExtensions.AndWhereToken }, out token))
            {
                bool anyCriteria = false;
                
                var baseProps = query.GetType().BaseType.GetProperties().Select(pi => pi.Name);
                var builtInParams = sql.GetParameterNames(true).Select(p => p.ToLower());
                
                // loop through this query's properties, but ignore base properties (like ResolvedSql and TraceCallback) since they are never part of WHERE clause                
                foreach (var pi in query.GetType().GetProperties().Where(pi => !baseProps.Contains(pi.Name)))
                {
                    object value = pi.GetValue(query);
                    if (value != null)
                    {
                        parameters.Add(new QueryTrace.Parameter() { Name = pi.Name, Value = value });

                        // built-in params are not part of the WHERE clause, so they are excluded from added terms
                        if (!builtInParams.Contains(pi.Name.ToLower()))
                        {
                            WhereAttribute whereAttr = pi.GetAttribute<WhereAttribute>();
                            string expression = (whereAttr != null) ? whereAttr.Expression : $"{query._db.Syntax.ApplyDelimiter(pi.Name)}=@{pi.Name}";
                            terms.Add(expression);
                            anyCriteria = true;
                        }
                    }
                }
                result = result.Replace(token, (anyCriteria) ? $"{whereBuilder[token]} {string.Join(" AND ", terms)}" : string.Empty);
            }

            return result;
        }
    }
}