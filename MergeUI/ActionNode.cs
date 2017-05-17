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
        private readonly Orm.Merge.Action.MergeAction _action;

        public ActionNode(Orm.Merge.Action.MergeAction action) : base(action.ToString())
        {
            _action = action;
            ImageKey = action.ObjectType.ToString();
            SelectedImageKey = action.ObjectType.ToString();
        }

        public int StartLine { get; set; }
        public int EndLine { get; set; }

        public Orm.Merge.Action.MergeAction Action { get { return _action; } }
    }
}
