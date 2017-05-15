using Postulate.Orm.Abstract;
using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb>
    {
        /// <summary>
        /// Drops and rebuilds unique keys when included columns or types have changed
        /// </summary>
        private IEnumerable<MergeAction> AlterUniqueKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
