using Postulate.Orm.Attributes;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge.Action;
using ReflectionHelper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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