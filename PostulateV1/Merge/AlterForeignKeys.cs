using Postulate.Abstract;
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
        /// Drops and rebuilds foreign keys whose cascade delete option has changed
        /// </summary>
        private IEnumerable<SchemaDiff> AlterForeignKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
