using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Postulate.Orm
{
	/// <summary>
	/// Provides strong-typed access to inline SQL queries, with dynamic criteria and tracing capability
	/// </summary>
	/// <typeparam name="TResult">Type with properties that map to columns returned by the query</typeparam>
	public class Query<TResult>
	{
		private readonly string _sql;
		private readonly ISqlDb _db;
		private string _resolvedSql;

		/// <summary>
		/// Set this to receive metrics about the last query executed, including the user name, full SQL, parameter info, and duration
		/// </summary>
		public Action<IDbConnection, QueryTrace> TraceCallback { get; set; }

		/// <summary>
		/// Constructor without the ISqlDb argument requires open connection when executing
		/// </summary>
		public Query(string sql)
		{
			_sql = sql;
		}

		public Query(string sql, ISqlDb db)
		{
			_sql = sql;
			_db = db;
		}

		protected ISqlDb Db { get { return _db; } }

		public int RowsPerPage { get; set; } = 50;

		public string Sql { get { return _sql; } }

		public string ResolvedSql { get { return _resolvedSql; } }

		public int CommandTimeout { get; set; } = 30;

		public CommandType CommandType { get; set; } = CommandType.Text;

		/// <summary>
		/// Lets you specify where in the application a query is called so that trace information is potentially more useful
		/// </summary>
		public string TraceContext { get; set; }

		public IEnumerable<TResult> Execute(int pageNumber = 0)
		{
			using (var cn = _db.GetConnection())
			{
				cn.Open();
				return ExecuteInner(cn, pageNumber);
			}
		}

		public IEnumerable<TResult> Execute(IDbConnection connection, int pageNumber = 0)
		{
			return ExecuteInner(connection, pageNumber);
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
			_resolvedSql = ResolveQuery(_sql, this, 0, out List<QueryTrace.Parameter> parameters, out DynamicParameters queryParams);
			_resolvedSql = OnQueryResolved(connection, _resolvedSql, queryParams);

			Stopwatch sw = Stopwatch.StartNew();
			TResult result = connection.QueryFirstOrDefault<TResult>(_resolvedSql, queryParams);
			sw.Stop();

			InvokeTraceCallback(connection, parameters, sw);

			return result;
		}

		private void InvokeTraceCallback(IDbConnection connection, List<QueryTrace.Parameter> parameters, Stopwatch sw)
		{
			TraceCallback?.Invoke(connection, new QueryTrace(GetType().FullName, _db.UserName, _resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext));
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
			_resolvedSql = ResolveQuery(_sql, this, -1, out List<QueryTrace.Parameter> parameters, out DynamicParameters queryParams);
			_resolvedSql = OnQueryResolved(connection, _resolvedSql, queryParams);

			Stopwatch sw = Stopwatch.StartNew();
			TResult result = await connection.QueryFirstOrDefaultAsync<TResult>(_resolvedSql, queryParams);
			sw.Stop();

			InvokeTraceCallback(connection, parameters, sw);

			return result;
		}

		public async Task<IEnumerable<TResult>> ExecuteAsync(int pageNumber = 0)
		{
			using (var cn = _db.GetConnection())
			{
				cn.Open();
				return await ExecuteInnerAsync(cn, pageNumber);
			}
		}

		public async Task<IEnumerable<TResult>> ExecuteAsync(IDbConnection connection, int pageNumber = 0)
		{
			return await ExecuteInnerAsync(connection, pageNumber);
		}

		private IEnumerable<TResult> ExecuteInner(IDbConnection connection, int pageNumber = 0)
		{
			IEnumerable<TResult> results = null;

			_resolvedSql = ResolveQuery(_sql, this, pageNumber, out List<QueryTrace.Parameter> parameters, out DynamicParameters queryParams);
			_resolvedSql = OnQueryResolved(connection, _resolvedSql, queryParams);

			Stopwatch sw = Stopwatch.StartNew();
			results = connection.Query<TResult>(_resolvedSql, queryParams, commandTimeout: CommandTimeout);
			sw.Stop();

			InvokeTraceCallback(connection, parameters, sw);

			return results;
		}

		private async Task<IEnumerable<TResult>> ExecuteInnerAsync(IDbConnection connection, int pageNumber = 0)
		{
			IEnumerable<TResult> results = null;

			_resolvedSql = ResolveQuery(_sql, this, pageNumber, out List<QueryTrace.Parameter> parameters, out DynamicParameters queryParams);
			_resolvedSql = OnQueryResolved(connection, _resolvedSql, queryParams);

			Stopwatch sw = Stopwatch.StartNew();
			results = await connection.QueryAsync<TResult>(_resolvedSql, queryParams, commandTimeout: CommandTimeout);
			sw.Stop();

			InvokeTraceCallback(connection, parameters, sw);

			return results;
		}

		private static string ResolveQuery(string sql, Query<TResult> query, int pageNumber, out List<QueryTrace.Parameter> parameters, out DynamicParameters queryParams)
		{
			string result = sql;
			List<string> terms = new List<string>();
			parameters = new List<QueryTrace.Parameter>();

			// this gets the param names within the query based on words with leading '@'
			var builtInParams = sql.GetParameterNames(true).Select(p => p.ToLower());

			// these are properties of the Query base type that we ignore because they are never part of WHERE clause (things like CommandType and CommandTimeout)
			var baseProps = query.GetType().BaseType.GetProperties().Select(pi => pi.Name);

			// these are the properties of the Query that are explicitly defined and may impact the WHERE clause
			var queryProps = query.GetType().GetProperties().Where(pi => !baseProps.Contains(pi.Name));

			queryParams = new DynamicParameters();
			foreach (var prop in queryProps)
			{
				var value = prop.GetValue(query);
				if (value != null) queryParams.Add(prop.Name, value);
			}

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
					if (HasValue(value))
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

			if (pageNumber > 0)
			{
				result = query.Db.Syntax.ApplyPaging(result, pageNumber, query.RowsPerPage);
			}

			return result;
		}

        private static bool HasValue(object value)
        {
            if (value != null)
            {
                if (value.Equals(string.Empty)) return false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Override this to make any changes to the query after it's been resolved,
        /// such as replacing macros or injecting environment-specific content based on the connection.
        /// You can also inspect parameeters that are passed
        /// </summary>
        protected virtual string OnQueryResolved(IDbConnection connection, string query, DynamicParameters parameters)
		{
			return query;
		}
	}
}