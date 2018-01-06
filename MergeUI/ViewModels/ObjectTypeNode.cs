using Postulate.Orm.ModelMerge;
using System.Windows.Forms;

namespace Postulate.MergeUI.ViewModels
{
    internal class ObjectTypeNode : TreeNode
    {
        public ObjectTypeNode(ObjectType objectType, int count) : base($"{objectType.ToString()} ({count})")
        {
            ImageKey = objectType.ToString();
            SelectedImageKey = objectType.ToString();
        }
    }
}