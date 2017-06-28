using Postulate.Orm.Extensions;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Abstract
{
    public abstract class SeedData<TRecord, TKey> where TRecord : Record<TKey>
    {
        public abstract string ExistsCriteria { get; }
        public abstract IEnumerable<TRecord> Records { get; }

        public void Generate(IDbConnection connection, SqlDb<TKey> db)
        {
            foreach (var record in Records)
            {
                if (!connection.Exists(ExistsCriteria, record)) db.Save(record);
            }
        }

        public void Generate(SqlDb<TKey> db)
        {
            using (var cn = db.GetConnection())
            {
                cn.Open();
                Generate(cn, db);
            }
        }
    }
}
