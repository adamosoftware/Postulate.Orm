using Postulate.Orm.Extensions;
using System.Collections.Generic;

namespace Postulate.Orm.Abstract
{
    public abstract class SeedData<TRecord, TKey> where TRecord : Record<TKey>
    {
        public abstract string ExistsCriteria { get; }
        public abstract IEnumerable<TRecord> Records { get; }

        public void Generate(SqlDb<TKey> db)
        {
            using (var cn = db.GetConnection())
            {
                cn.Open();
                foreach (var record in Records)
                {
                    if (!cn.Exists(ExistsCriteria, record)) db.Save(record);
                }
            }
        }
    }
}
