using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Postulate.Enums;

namespace Postulate
{
    public class SqlServerDb<TKey> : SqlDb<TKey>
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
    }
}
