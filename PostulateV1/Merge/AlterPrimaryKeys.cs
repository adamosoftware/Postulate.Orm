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
        /// Drops and rebuilds primary keys when the columns or types of included columns have changed
        /// </summary>
        private IEnumerable<SchemaDiff> AlterPrimaryKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
