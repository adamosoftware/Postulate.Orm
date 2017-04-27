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
        /// Creates tables and columns that exist in the model but not in the schema
        /// </summary>
        private IEnumerable<Diff> CreateTablesAndColumns(IDbConnection connection)
        {
            // tables and columns together so that we can easily exclude columns from created tables without relying on instance variable
            throw new NotImplementedException();

            // create tables...

            // alter table add...
        }
    }
}
