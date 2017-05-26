using Postulate.Orm.Attributes;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge.Action;
using ReflectionHelper;
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
            var renamedColumns = _modelTypes.SelectMany(t => t.GetProperties().Where(pi => pi.HasAttribute<RenameFromAttribute>()));

            List<RenameColumn> results = new List<RenameColumn>();
            results.AddRange(renamedColumns.Select(pi => new RenameColumn(pi)));
            return results;
        }
    }
}
