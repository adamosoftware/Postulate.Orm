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
    /// <summary>
    /// Provides strong-typed access to inline SQL queries, with dynamic criteria and tracing capability
    /// </summary>
    /// <typeparam name="TResult">Type with properties that map to columns returned by the query</typeparam>
    public abstract class Query<TResult>
    {
        private readonly string _sql;      
        private readonly IDb _db;
        private string _resolvedSql;

        /// <summary>
        /// Set this to receive metrics about the last query executed, including the user name, full SQL, parameter info, and duration
        /// </summary>
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

        public TResult ExecuteSingle()
        {
            using (var cn = _db.GetConnection())
            {
                cn.Open();
                return ExecuteSingle(cn);
            }
        }

        public TResult ExecuteSingle(IDbConnection connection)
        {
            List<QueryTrace.Parameter> parameters;
            _resolvedSql = ResolveQuery(_sql, this, out parameters);

            Stopwatch sw = Stopwatch.StartNew();
            TResult result = connection.QueryFirstOrDefault<TResult>(_resolvedSql, this);
            sw.Stop();

            TraceCallback?.Invoke(connection, new QueryTrace(GetType().FullName, _db.UserName, _resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext));

            return result;
        }

        public async Task<TResult> ExecuteSingleAsync()
        {
            using (var cn = _db.GetConnection())
            {
                cn.Open();
                return await ExecuteSingleAsync(cn);
            }
        }

        public async Task<TResult> ExecuteSingleAsync(IDbConnection connection)
        {
            List<QueryTrace.Parameter> parameters;
            _resolvedSql = ResolveQuery(_sql, this, out parameters);

            Stopwatch sw = Stopwatch.StartNew();
            TResult result = await connection.QueryFirstOrDefaultAsync<TResult>(_resolvedSql, this);
            sw.Stop();

            TraceCallback?.Invoke(connection, new QueryTrace(GetType().FullName, _db.UserName, _resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext));

            return result;
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

            TraceCallback?.Invoke(connection, new QueryTrace(GetType().FullName, _db.UserName, _resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext));

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

            TraceCallback?.Invoke(connection, new QueryTrace(GetType().FullName, _db.UserName, _resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext));

            return results;
        }

        private static string ResolveQuery(string sql, Query<TResult> query, out List<QueryTrace.Parameter> parameters)
        {
            string result = sql;
            List<string> terms = new List<string>();
            parameters = new List<QueryTrace.Parameter>();

            var builtInParams = sql.GetParameterNames(true).Select(p => p.ToLower());
            var baseProps = query.GetType().BaseType.GetProperties().Select(pi => pi.Name);
            var queryProps = query.GetType().GetProperties().Where(pi => !baseProps.Contains(pi.Name));

            Dictionary<string, string> whereBuilder = new Dictionary<string, string>()
            {
                { InternalStringExtensions.WhereToken, "WHERE" }, // query has no where clause, so it needs the word WHERE inserted
                { InternalStringExtensions.AndWhereToken, "AND" } // query already contains a WHERE clause, we're just adding to it
            };
            string token;
            if (result.ContainsAny(new string[] { InternalStringExtensions.WhereToken, InternalStringExtensions.AndWhereToken }, out token))
            {
                bool anyCriteria = false;                                
                               
                // loop through this query's properties, but ignore base properties (like ResolvedSql and TraceCallback) since they are never part of WHERE clause                
                foreach (var pi in queryProps)
                {
                    object value = pi.GetValue(query);
                    if (value != null)
                    {
                        parameters.Add(new QueryTrace.Parameter() { Name = pi.Name, Value = value });

                        // built-in params are not part of the WHERE clause, so they are excluded from added terms
                        if (!builtInParams.Contains(pi.Name.ToLower()))
                        {
                            anyCriteria = true;

                            var cases = pi.GetCustomAttributes(typeof(CaseAttribute), false).OfType<CaseAttribute>();
                            var selectedCase = cases?.FirstOrDefault(c => c.Value.Equals(value));
                            if (selectedCase != null)
                            {
                                terms.Add(selectedCase.Expression);
                            }
                            else
                            {
                                WhereAttribute whereAttr = pi.GetAttribute<WhereAttribute>();
                                string expression = (whereAttr != null) ? whereAttr.Expression : $"{query._db.Syntax.ApplyDelimiter(pi.Name)}=@{pi.Name}";
                                terms.Add(expression);                                
                            }
                        }
                    }
                }
                result = result.Replace(token, (anyCriteria) ? $"{whereBuilder[token]} {string.Join(" AND ", terms)}" : string.Empty);
            }
            
            // need to add built in params explicitly to outgoing param list for the benefit of QueryTrace
            foreach (var parameter in builtInParams)
            {
                var paramProperty = queryProps.Single(pi => pi.Name.ToLower().Equals(parameter));
                parameters.Add(new QueryTrace.Parameter() { Name = parameter, Value = paramProperty.GetValue(query) });
            }

            return result;
        }        
    }
}