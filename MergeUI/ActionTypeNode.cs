using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    internal class ActionTypeNode : TreeNode
    {
        public ActionTypeNode(MergeActionType actionType, int count) : base($"{actionType.ToString()} ({count})")
        {
            ImageKey = actionType.ToString();
            SelectedImageKey = actionType.ToString();
        }
    }
}
