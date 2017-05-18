using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge
{
    internal class ForeignKeyRef
    {
        public string ConstraintName { get; set; }
        public ColumnRef Parent { get; set; }
        public ColumnRef Child { get; set; }        
        public DbObject ChildObject { get; set; }
    }

    internal class ForeignKeyInfo
    {
        public string ConstraintName { get; set; }
        public string ReferencedSchema { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
        public string ReferencingSchema { get; set; }
        public string ReferencingTable { get; set; }
        public string ReferencingColumn { get; set; }
    }
}
