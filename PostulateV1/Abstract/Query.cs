using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;

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

        public Query(string sql, Func<IDbConnection> connectionGetter, int sortIndex = -1)
        {
            _sql = sql;
            _connectionGetter = connectionGetter;
        }

        public string Sql { get { return _sql; } }

        public int CommandTimeout { get; set; } = 30;

        public CommandType CommandType { get; set; } = CommandType.Text;

        public virtual string[] SortOptions { get { return null; } }

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
            return connection.Query<TResult>(ResolveQuery(_sql, sortIndex), this, commandType: CommandType);
        }

        private string ResolveQuery(string sql, int sortIndex)
        {
            if (sortIndex > -1)
            {
                if (!sql.Contains("{orderBy}") || SortOptions == null) throw new ArgumentException("To use the Query sortIndex argument, the SortOptions property must be set, and \"{orderBy}\" must appear in the SQL command.");
                return sql.Replace("{orderBy}", $"ORDER BY {SortOptions[sortIndex]}");
            }
            return sql;
        }

        private string GetSortOption(int sortIndex)
        {
            try
            {
                return SortOptions[sortIndex];
            }
            catch (IndexOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Sort index {sortIndex} is out of range of the defined sort options for this query.");
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("The SortOptions property returned null.");
            }
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
    }
}
