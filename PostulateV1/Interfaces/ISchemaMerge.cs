using Postulate.Orm.Merge;
using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Interfaces
{
    public interface ISchemaMerge
    {
        IEnumerable<MergeAction> Compare(IDbConnection connection);        
        StringBuilder GetScript(IDbConnection connection, IEnumerable<MergeAction> actions, out Dictionary<MergeAction, LineRange> lineRanges);
        void SaveScriptAs(IDbConnection connection, string fileName);        
        void Execute(IDbConnection connection);
        void Execute(IDbConnection connection, IEnumerable<MergeAction> actions);
    }
}
