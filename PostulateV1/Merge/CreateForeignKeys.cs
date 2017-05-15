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
        /// Creates foreign keys that exist in the model but not in the schema
        /// </summary>
        private IEnumerable<MergeAction> CreateForeignKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
