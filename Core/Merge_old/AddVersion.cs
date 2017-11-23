using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb>
    {
        /// <summary>
        /// Adds the model version number and change info to the meta.Version table
        /// </summary>
        private MergeAction ScriptVersionInfo(IEnumerable<MergeAction> changes)
        {
            throw new NotImplementedException();
        }
    }
}