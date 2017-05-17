using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge;
using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.MergeUI
{
    internal class MergeInfo
    {
        public IDb Db { get; set; }
        public ISchemaMerge Merge { get; set; }
        public string ServerAndDatabase { get; set; }
        public IEnumerable<MergeAction> Actions { get; set; }
        public Dictionary<MergeAction, LineRange> LineRanges { get; set; }
        public StringBuilder Script { get; set; }
    }
}
