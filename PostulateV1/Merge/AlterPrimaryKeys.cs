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
        /// Drops and rebuilds primary keys when the columns or types of included columns have changed
        /// </summary>
        private IEnumerable<MergeAction> AlterPrimaryKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
