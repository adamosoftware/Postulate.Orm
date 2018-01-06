using Postulate.Orm.ModelMerge;
using System.Windows.Forms;

namespace Postulate.MergeUI.ViewModels
{
    internal class ActionTypeNode : TreeNode
    {
        public ActionTypeNode(ActionType actionType, int count) : base($"{actionType.ToString()} ({count})")
        {
            ImageKey = actionType.ToString();
            SelectedImageKey = actionType.ToString();
        }
    }
}