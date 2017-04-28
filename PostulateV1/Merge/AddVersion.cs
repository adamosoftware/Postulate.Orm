using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
    public partial class SchemaMerge<TDb, TKey> where TDb : SqlDb<TKey>, new()
    {
        /// <summary>
        /// Adds the model version number and change info to the meta.Version table
        /// </summary>
        private Diff ScriptVersionInfo(IEnumerable<SchemaDiff> changes)
        {
            throw new NotImplementedException();
        }
    }
}
