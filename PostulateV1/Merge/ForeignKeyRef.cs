using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge
{
    internal class ForeignKeyRef
    {
        public DbObject ReferencingTable { get; set; }
        public string ConstraintName { get; set; }
    }
}
