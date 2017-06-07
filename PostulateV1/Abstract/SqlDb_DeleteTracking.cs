using Dapper;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        protected abstract void OnCaptureDeletion<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transasction) where TRecord : Record<TKey>;

        protected abstract TRecord BeginRestore<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>;

        protected abstract void CompleteRestore<TRecord>(IDbConnection connection, TKey id, IDbTransaction transaction) where TRecord : Record<TKey>;
    }
}
