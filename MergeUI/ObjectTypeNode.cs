using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    internal class ObjectTypeNode : TreeNode
    {
        public ObjectTypeNode(MergeObjectType objectType, int count) : base($"{objectType.ToString()} ({count})")
        {
            ImageKey = objectType.ToString();
            SelectedImageKey = objectType.ToString();
        }
    }
}
