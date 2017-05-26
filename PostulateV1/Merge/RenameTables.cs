using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge.Action;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using ReflectionHelper;
using Postulate.Orm.Attributes;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb> : ISchemaMerge where TDb : IDb, new()
    {
        private IEnumerable<MergeAction> RenameTables(IDbConnection connection)
        {
            var renamedTables = _modelTypes.Where(t => t.HasAttribute<RenameFromAttribute>());

            List<RenameTable> results = new List<RenameTable>();
            results.AddRange(renamedTables.Select(rt => new RenameTable(rt)));
            return results;
        }

    }
}
