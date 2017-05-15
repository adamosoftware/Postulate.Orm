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
        /// Creates primary keys on existing tables that lack an explicit primary key
        /// </summary>
        private IEnumerable<MergeAction> CreatePrimaryKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

    }
}
