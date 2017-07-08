using Postulate.Orm.Interfaces;
using System.Data;
using Dapper;
using Postulate.Orm.Exceptions;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        public TRecord Find<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            var row = ExecuteFind<TRecord>(connection, id);
            return FindInner(connection, row);
        }

        public TRecord Find<TRecord>(IDbConnection connection, TKey id, out int version) where TRecord : Record<TKey>
        {
            var record = Find<TRecord>(connection, id);
            version = GetRecordNextVersion<TRecord>(connection, id);
            return FindInner(connection, record);
        }

        public TRecord Find<TRecord>(TKey id) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return Find<TRecord>(cn, id);
            }
        }

        public TRecord Find<TRecord>(TKey id, out int version) where TRecord : Record<TKey>
        {
            using (var cn = GetConnection())
            {
                cn.Open();
                var record = Find<TRecord>(cn, id);
                version = GetRecordNextVersion<TRecord>(cn, id);
                return FindInner(cn, record);
            }
        }

        public TRecord FindWhere<TRecord>(IDbConnection connection, string critieria, object parameters) where TRecord : Record<TKey>
        {
            var row = ExecuteFindWhere<TRecord>(connection, critieria, parameters);
            return FindInner(connection, row);
        }

        public TRecord FindUserProfile<TRecord>(string userName = null) where TRecord : Record<TKey>, IUserProfile
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return FindUserProfile<TRecord>(cn, userName);
            }
        }

        public TRecord FindUserProfile<TRecord>(IDbConnection connection, string userName = null) where TRecord : Record<TKey>, IUserProfile
        {
            return ExecuteFindWhere<TRecord>(connection, "[UserName]=@name", new { name = userName ?? UserName });
        }

        public TRecord FindWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return FindWhere<TRecord>(cn, criteria, parameters);
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

        public bool ExistsWhere<TRecord>(IDbConnection connection, string criteria, object parameters) where TRecord : Record<TKey>
        {
            string cmd = $"SELECT 1 FROM {GetTableName<TRecord>()} WHERE {criteria}";
            int result = connection.QueryFirstOrDefault<int?>(cmd, parameters) ?? 0;
            return (result == 1);
        }

        private TRecord FindInner<TRecord>(IDbConnection connection, TRecord row) where TRecord : Record<TKey>
        {
            if (row == null) return null;

            string message;
            if (row.AllowView(connection, UserName, out message))
            {
                row.BeforeView(connection, this);
                return row;
            }
            else
            {
                throw new PermissionDeniedException(message);
            }
        }

        private TRecord ExecuteFind<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_findCommands, () => GetFindStatement<TRecord>());
            return ExecuteFindMethod<TRecord>(connection, id, cmd);
        }

        protected virtual TRecord ExecuteFindMethod<TRecord>(IDbConnection connection, TKey id, string cmd) where TRecord : Record<TKey>
        {
            return connection.QueryFirstOrDefault<TRecord>(cmd, new { id = id });
        }

        private TRecord ExecuteFindWhere<TRecord>(IDbConnection connection, string criteria, object parameters) where TRecord : Record<TKey>
        {
            string cmd = GetFindStatementBase<TRecord>() + $" WHERE {criteria}";
            return ExecuteFindWhereMethod<TRecord>(connection, parameters, cmd);
        }

        protected virtual TRecord ExecuteFindWhereMethod<TRecord>(IDbConnection connection, object parameters, string cmd) where TRecord : Record<TKey>
        {
            return connection.QuerySingleOrDefault<TRecord>(cmd, parameters);
        }
    }
}