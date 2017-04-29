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
        private IEnumerable<MergeAction> DropTables(IDbConnection connection)
        {
            List<MergeAction> results = new List<MergeAction>();

            var schemaTables = GetSchemaTables(connection);
            var dropTables = schemaTables.Where(obj => !_modelTypes.Any(t => obj.Equals(t)));

            results.AddRange(dropTables.Select(obj => new DropTable(obj, $"Table {obj} was dropped from model")));

            return results;
        }
    }
}
