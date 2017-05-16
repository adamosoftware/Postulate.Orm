using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    internal class ActionNode : TreeNode
    {
        public ActionNode(MergeObjectType objectType, string text) : base(text)
        {
            ImageKey = objectType.ToString();
            SelectedImageKey = objectType.ToString();
        }

        public int StartLine { get; set; }
        public int EndLine { get; set; }
    }
}
