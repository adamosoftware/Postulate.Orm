using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge.Action;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb> : ISchemaMerge where TDb : IDb, new()
    {
        private IEnumerable<MergeAction> CreateSchemas(IDbConnection connection)
        {
            List<MergeAction> results = new List<MergeAction>();

            var schemas = _modelTypes
                .Select(t => t.GetSchema())
                .GroupBy(item => item)
                .Where(grp => !connection.SchemaExists(grp.Key));

            results.AddRange(schemas.Select(s => new CreateSchema(s.Key)));

            return results;
        }

    }
}
