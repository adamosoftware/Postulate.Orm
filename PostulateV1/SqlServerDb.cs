using Postulate.Abstract;
using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Postulate.Enums;
using System.Linq.Expressions;
using Postulate.Interfaces;

namespace Postulate
{
    public class SqlServerDb<TKey> : SqlDb<TKey>, IDb
    {
        public SqlServerDb(string connectionName, string userName = null) : base(connectionName)
        {
            UserName = userName;
        }

        protected override string ApplyDelimiter(string name)
        {
            return string.Join(".", name.Split('.').Select(s => $"[{s}]"));
        }

        public override IDbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public TRecord Find<TRecord>(TKey id) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return Find<TRecord>(cn, id);
            }
        }

        public bool ExistsWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return ExistsWhere<TRecord>(cn, criteria, parameters);
            }
        }

        public TRecord FindWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return FindWhere<TRecord>(cn, criteria, parameters);
            }
        }

        public void Delete<TRecord>(TRecord record) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Delete(cn, record);
            }
        }

        public void Delete<TRecord>(TKey id) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Delete<TRecord>(cn, id);
            }
        }

        public void Save<TRecord>(TRecord record, out SaveAction action) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Save(cn, record, out action);
            }
        }

        public void Save<TRecord>(TRecord record) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                SaveAction action;
                Save(cn, record, out action);
            }
        }

        public void Update<TRecord>(TRecord record, params Expression<Func<TRecord, object>>[] setColumns) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Update(cn, record, setColumns);
            }
        }
    }
}
