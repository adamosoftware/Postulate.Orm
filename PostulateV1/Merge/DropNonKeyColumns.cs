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
        /// Drops columns that exist in the schema but not the model that are not part of a key
        /// </summary>
        private IEnumerable<Diff> DropNonKeyColumns(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
