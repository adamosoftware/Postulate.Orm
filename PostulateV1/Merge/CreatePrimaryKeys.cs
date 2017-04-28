using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
    public partial class SchemaMerge<TDb, TKey> where TDb : SqlDb<TKey>, new()
    {
        /// <summary>
        /// Creates primary keys on existing tables that lack an explicit primary key
        /// </summary>
        private IEnumerable<SchemaDiff> CreatePrimaryKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

    }
}
