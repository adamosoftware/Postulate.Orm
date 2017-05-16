using Postulate.Orm.Merge;
using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.MergeUI.ViewModels
{
    internal class MergeTree
    {
        private readonly IEnumerable<IGrouping<MergeActionType, MergeAction>> _actionTypes;

        public MergeTree(IEnumerable<MergeAction> actions)
        {
            _actionTypes = actions.GroupBy(item => item.ActionType);
            
        }


    }
}
