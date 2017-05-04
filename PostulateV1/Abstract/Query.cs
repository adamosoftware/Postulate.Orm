using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using Postulate.Extensions;
using Postulate.Attributes;

namespace Postulate.Abstract
{
    /// <summary>
    /// Defines a SQL query with a fixed parameter set
    /// </summary>
    /// <typeparam name="TResult">Type of result</typeparam>
    public abstract class Query<TResult>
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

        /// <summary>
        /// Defines a list of possible ORDER BY clauses that can be used with a query, enabling end-user sort choice without exposing a SQL injection risk
        /// </summary>
        public virtual SortOption[] SortOptions { get { return null; } }

        private string ResolveQuery(int sortIndex)
        {
            const string orderByToken = "{orderBy}";
            const string whereToken = "{where}";
            const string andWhereToken = "{andWhere}";

            string result = _sql;

            if (result.Contains(orderByToken))
            {
                if (sortIndex > -1)
                {
                    if (!result.Contains(orderByToken) || SortOptions == null) throw new ArgumentException("To use the Query sortIndex argument, the SortOptions property must be set, and \"{orderBy}\" must appear in the SQL command.");
                    result = result.Replace(orderByToken, $"ORDER BY {GetSortOption(sortIndex)}");
                }
                else
                {
                    result = result.Replace(orderByToken, string.Empty);
                }                
            }

            Dictionary<string, string> whereBuilder = new Dictionary<string, string>()
            {
                { whereToken, "WHERE" }, // query has no where clause, so it needs the word WHERE inserted
                { andWhereToken, "AND" } // query already contains a WHERE clause, we're just adding to it
            };
            string token;
            if (result.ContainsAny(new string[] { whereToken, andWhereToken }, out token))
            {
                bool anyCriteria = false;
                List<string> terms = new List<string>();
                var props = GetType().GetProperties().Where(pi => pi.HasAttribute<WhereAttribute>());
                foreach (var pi in props)
                {
                    anyCriteria = true;
                    WhereAttribute whereAttr = pi.GetCustomAttributes(false).OfType<WhereAttribute>().First();
                    object value = pi.GetValue(this);
                    if (value != null) terms.Add(whereAttr.Expression);
                }
                result = result.Replace(whereToken, (anyCriteria) ? $"{whereBuilder[token]} {string.Join(" AND ", terms)}" : string.Empty);
            }

            return result;
        }

        /// <summary>
        /// Gets the sort expression for a given sortIndex argument. Handles a couple of common exceptions so that a useful error message is returned
        /// </summary>
        private string GetSortOption(int sortIndex)
        {
            try
            {
                return SortOptions[sortIndex].Expression;
            }
            catch (IndexOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Sort index {sortIndex} is out of range of the defined sort options for query {GetType().Name}.");
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException($"The SortOptions property is not set on query {GetType().Name}.");
            }
        }

        public IEnumerable<TResult> Execute(int sortIndex = -1)
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return Execute(cn, sortIndex);
            }
        }

        public IEnumerable<TResult> Execute(IDbConnection connection, int sortIndex = -1)
        {
            return connection.Query<TResult>(ResolveQuery(sortIndex), this, commandType: CommandType);
        }

        public IEnumerable<TResult> Execute(string orderBy, int pageSize, int pageNumber)
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return Execute(cn, orderBy, pageSize, pageNumber);
            }
        }

        public IEnumerable<TResult> Execute(int sortIndex, int pageSize, int pageNumber)
        {
            return Execute(GetSortOption(sortIndex), pageSize, pageNumber);
        }

        public IEnumerable<TResult> Execute(IDbConnection connection, string orderBy, int pageSize, int pageNumber)
        {
            string query = BuildPagedQuery(_sql, orderBy, pageSize, pageNumber);
            return connection.Query<TResult>(query, this);
        }

        public IEnumerable<TResult> Execute(IDbConnection connection, int sortIndex, int pageSize, int pageNumber)
        {
            return Execute(connection, GetSortOption(sortIndex), pageSize, pageNumber);
        }

        public static string BuildPagedQuery(string query, string orderBy, int pageSize, int pageNumber)
        {
            int startRecord = (pageNumber * pageSize) + 1;
            int endRecord = (pageNumber * pageSize) + pageSize;
            return $"WITH [source] AS ({InsertRowNumberColumn(query, orderBy)}) SELECT * FROM [source] WHERE [RowNumber] BETWEEN {startRecord} AND {endRecord};";
        }

        public static string InsertRowNumberColumn(string query, string orderBy)
        {
            StringBuilder sb = new StringBuilder(query);
            int insertPoint = query.ToLower().IndexOf("select ") + "select ".Length;
            sb.Insert(insertPoint, $"ROW_NUMBER() OVER(ORDER BY {orderBy}) AS [RowNumber], ");
            return sb.ToString();
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(int sortIndex = -1)
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return await ExecuteAsync(cn);
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(IDbConnection connection, int sortIndex = -1)
        {
            return await connection.QueryAsync<TResult>(_sql, this, commandType: CommandType);
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(IDbConnection connection, string orderBy, int pageSize, int pageNumber)
        {
            string query = BuildPagedQuery(_sql, orderBy, pageSize, pageNumber);
            return await connection.QueryAsync<TResult>(query, this);
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(IDbConnection connection, int sortIndex, int pageSize, int pageNumber)
        {
            return await ExecuteAsync(connection, GetSortOption(sortIndex), pageSize, pageNumber);
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(string orderBy, int pageSize, int pageNumber)
        {
            using (IDbConnection cn = _connectionGetter.Invoke())
            {
                cn.Open();
                return await ExecuteAsync(cn, orderBy, pageSize, pageNumber);
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(int sortIndex, int pageSize, int pageNumber)
        {
            return await ExecuteAsync(GetSortOption(sortIndex), pageSize, pageNumber);
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

        public void Test(IDbConnection connection)
        {
            string context = "No arguments";
            try
            {                
                var results = Execute(connection);

                if (SortOptions != null)
                {
                    for (int i = 0; i < SortOptions.Length; i++)
                    {
                        context = $"Sort option: {SortOptions[i].Expression}";
                        results = Execute(connection, i);
                    }
                }

                foreach (var pi in GetType().GetProperties().Where(pi => pi.HasAttribute<WhereAttribute>(attr => attr.TestValue != null)))
                {
                    context = $"Property: {pi.Name}";
                    WhereAttribute attr = pi.GetCustomAttributes(false).OfType<WhereAttribute>().First();
                    pi.SetValue(this, attr.TestValue);
                    results = Execute(connection);
                }
            }
            catch (Exception exc)
            {
                throw new Exception($"Error in query {GetType().Name}: {exc.Message} ({context})");
            }
        }

        public class SortOption
        {
            public string Text { get; set; }
            public string Expression { get; set; }

            public override string ToString()
            {
                return Expression;
            }
        }
    }
}
