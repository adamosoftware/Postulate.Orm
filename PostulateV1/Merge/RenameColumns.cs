using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb> : ISchemaMerge where TDb : IDb, new()
    {
        private IEnumerable<MergeAction> RenameColumns(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
