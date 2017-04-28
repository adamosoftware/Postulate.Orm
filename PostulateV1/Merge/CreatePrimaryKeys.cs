using Postulate.Abstract;
using Postulate.Merge.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
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
