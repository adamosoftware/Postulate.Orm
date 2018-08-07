using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.ModelMerge.Actions;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Util
{
	public static class QueryUtil
	{
		/// <summary>
		/// Provides a general-purpose way to save QueryTraces. Use this in <see cref="Query{TResult}.TraceCallback"/> or
		/// <see cref="SqlDb{TKey}.TraceCallback"/> handlers
		/// </summary>
		public static void SaveTrace(IDbConnection connection, QueryTrace trace, SqlDb<int> db)
		{
			if (!db.Syntax.TableExists(connection, typeof(QueryTrace)))
			{
				var tableInfo = db.Syntax.GetTableInfoFromType(typeof(QueryTrace));
				if (!db.Syntax.SchemaExists(connection, tableInfo.Schema))
				{
					CreateSchema cs = new CreateSchema(db.Syntax, tableInfo.Schema);
					foreach (var cmd in cs.SqlCommands(connection)) connection.Execute(cmd);
				}
				CreateTable ct = new CreateTable(db.Syntax, db.Syntax.GetTableInfoFromType(typeof(QueryTrace)));
				foreach (var cmd in ct.SqlCommands(connection)) connection.Execute(cmd);
			}

			var callback = db.TraceCallback;

			// prevent infinite loop
			db.TraceCallback = null;

			db.Save(connection, trace);

			// restore original callback
			db.TraceCallback = callback;
		}

		/// <summary>
		/// Returns the properties of a query object based on parameters defined in a 
		/// SQL statement as well as properties with Where and Case attributes
		/// </summary>
		public static IEnumerable<PropertyInfo> GetProperties(object query, string sql, out IEnumerable<string> builtInParams)
		{
			// this gets the param names within the query based on words with leading '@'
			builtInParams = sql.GetParameterNames(true).Select(p => p.ToLower());
			var builtInParamsArray = builtInParams.ToArray();

			// these are the properties of the Query that are explicitly defined and may impact the WHERE clause
			var queryProps = query.GetType().GetProperties().Where(pi =>
				pi.HasAttribute<WhereAttribute>() ||
				pi.HasAttribute<CaseAttribute>() ||
				pi.HasAttribute<AttachWhereAttribute>() ||
				builtInParamsArray.Contains(pi.Name.ToLower()));

			return queryProps;
		}		

		public static string GetWhereClause(object criteria)
		{
			return string.Join(" AND ", GetWhereClauseTerms(criteria));
		}

		public static List<string> GetWhereClauseTerms(object criteria)
		{
			var props = GetProperties(criteria, string.Empty, out IEnumerable<string> builtInParams);
			return GetWhereClauseTerms(criteria, props, builtInParams, new List<QueryTrace.Parameter>(), null);
		}

		public static List<string> GetWhereClauseTerms(object query, 
			IEnumerable<PropertyInfo> queryProps, IEnumerable<string> builtInParams, List<QueryTrace.Parameter> traceParams,
			Func<PropertyInfo, string> termBuilder)
		{
			List<string> results = new List<string>();

			foreach (var pi in queryProps)
			{
				object value = pi.GetValue(query);
				if (HasValue(value))
				{
					traceParams.Add(new QueryTrace.Parameter() { Name = pi.Name, Value = value });

					// built-in params are not part of the generated WHERE clause, so they are excluded from added terms
					if (!builtInParams.Contains(pi.Name.ToLower()))
					{						
						var cases = pi.GetCustomAttributes(typeof(CaseAttribute), false).OfType<CaseAttribute>();
						var selectedCase = cases?.FirstOrDefault(c => c.Value.Equals(value));
						if (selectedCase != null)
						{
							results.Add(selectedCase.Expression);
						}
						else
						{
							WhereAttribute whereAttr = pi.GetAttribute<WhereAttribute>();
							AttachWhereAttribute attachWhere = pi.GetAttribute<AttachWhereAttribute>();

							if (whereAttr != null)
							{								
								results.Add(whereAttr.Expression);
							}
							else if (attachWhere != null)
							{
								results.AddRange(GetWhereClauseTerms(value));
							}														
							else if (termBuilder != null)
							{
								results.Add(termBuilder.Invoke(pi));
							}							
						}
					}
				}
			}

			return results;
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
	}
}