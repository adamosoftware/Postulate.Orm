using Postulate.Orm.Interfaces;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        protected abstract void OnCaptureDeletion<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transasction) where TRecord : Record<TKey>;
    }
}
