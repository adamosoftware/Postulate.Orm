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
        private IEnumerable<SchemaDiff> AlterNonKeyColumnTypes(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
