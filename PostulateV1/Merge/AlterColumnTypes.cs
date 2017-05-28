using Postulate.Orm.Merge.Action;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb>
    {
        private IEnumerable<MergeAction> AlterColumnTypes(IDbConnection connection)
        {
            var typeChanges = from mc in GetModelColumns(connection)
                              join sc in GetSchemaColumns(connection) on mc equals sc
                              where !mc.GetDataTypeSyntax().Equals(sc.GetDataTypeSyntax())
                              select new { ModelColumn = mc, SchemaColumn = sc, ModelType = mc.ModelType };

            List<MergeAction> actions = new List<MergeAction>();

            foreach (var change in typeChanges)
            {
                actions.Add(new AlterColumn(change.ModelColumn, change.SchemaColumn));
            }

            return actions;
        }
    }
}