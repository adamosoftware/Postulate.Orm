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
        /// Drops and rebuilds indexes where included columns or types have changed
        /// </summary>
        private IEnumerable<Diff> AlterIndexes(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
