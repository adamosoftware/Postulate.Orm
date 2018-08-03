using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Models;
using Postulate.Orm.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Postulate.Orm
{
	/// <summary>
	/// Provides strong-typed access to inline SQL queries, with dynamic criteria and tracing capability
	/// </summary>
	/// <typeparam name="TResult">Type with properties that map to columns returned by the query</typeparam>
	public class Query<TResult>
	{
		private ISqlDb _db;
		private readonly string _sql;		
		private string _resolvedSql;

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

		public ISqlDb Db { get { return _db; } set { _db = value; } }

		public int RowsPerPage { get; set; } = 50;

		public string Sql { get { return _sql; } }

		public string ResolvedSql { get { return _resolvedSql; } }

		public int CommandTimeout { get; set; } = 30;

		public CommandType CommandType { get; set; } = CommandType.Text;

		/// <summary>
		/// Lets you specify where in the application a query is called so that trace information is potentially more useful
		/// </summary>
		public string TraceContext { get; set; }
		
		public IEnumerable<TResult> Execute(ISqlDb db, int pageNumber = 0)
		{
			Db = db;
			return Execute(pageNumber);
		}

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

		public TResult ExecuteSingle(ISqlDb db)
		{
			Db = db;
			return ExecuteSingle();
		}

		private void InvokeTraceCallback(IDbConnection connection, List<QueryTrace.Parameter> parameters, Stopwatch sw)
		{
			var trace = new QueryTrace(GetType().FullName, _db.UserName, _resolvedSql, parameters, sw.ElapsedMilliseconds, TraceContext);
			_db?.TraceCallback?.Invoke(connection, trace);
			if (_db.TraceQueries) _db.QueryTraces.Add(trace);
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
			List<string> terms = null;
			parameters = new List<QueryTrace.Parameter>();

			var queryProps = QueryUtil.GetProperties(query, sql, out IEnumerable<string> builtInParams);

			queryParams = new DynamicParameters();
			ResolveQueryParams(queryParams, query, queryProps);

			Dictionary<string, string> whereBuilder = new Dictionary<string, string>()
			{
				{ InternalStringExtensions.WhereToken, "WHERE" }, // query has no WHERE clause, so it will be added
				{ InternalStringExtensions.AndWhereToken, "AND" } // query already contains a WHERE clause, we're just adding to it
			};
			string token;
			if (result.ContainsAny(whereBuilder.Select(kp => kp.Key), out token))
			{				
				terms = QueryUtil.GetWhereClauseTerms(query, queryProps, builtInParams, parameters, (pi) => $"{query.Db.Syntax.ApplyDelimiter(pi.Name)}=@{pi.Name}");
				result = result.Replace(token, (terms.Any()) ? $"{whereBuilder[token]} {string.Join(" AND ", terms)}" : string.Empty);
			}

			// need to add built in params explicitly to outgoing param list for the benefit of QueryTrace
			foreach (var parameter in builtInParams)
			{
				var paramProperty = queryProps.Single(pi => pi.Name.ToLower().Equals(parameter));
				parameters.Add(new QueryTrace.Parameter() { Name = parameter, Value = paramProperty.GetValue(query) });
			}

			if (pageNumber > 0)
			{
				if (query.Db == null) throw new NullReferenceException("If you use a page number argument with Query, the Db property must be set so that the paging syntax can be applied to the query.");
				result = query.Db.Syntax.ApplyPaging(result, pageNumber, query.RowsPerPage);
			}

			return result;
		}

		private static void ResolveQueryParams(DynamicParameters queryParams, object query, IEnumerable<PropertyInfo> queryProps)
		{
			foreach (var prop in queryProps)
			{
				var value = prop.GetValue(query);
				if (value != null)
				{
					if (prop.HasAttribute<AttachWhereAttribute>())
					{
						var nestedProps = QueryUtil.GetProperties(value, string.Empty, out IEnumerable<string> builtInParams);
						ResolveQueryParams(queryParams, value, nestedProps);
					}
					else
					{
						queryParams.Add(prop.Name, value);
					}					
				}
			}
		}

		/// <summary>
		/// Override this to make any changes to the query after it's been resolved,
		/// such as replacing macros or injecting environment-specific content based on the connection.
		/// You can also inspect parameters that are passed
		/// </summary>
		protected virtual string OnQueryResolved(IDbConnection connection, string query, DynamicParameters parameters)
		{
			return query;
		}
	}
}