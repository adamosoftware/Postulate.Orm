using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    internal class ActionNode : TreeNode
    {
        private readonly Orm.Merge.Action.MergeAction _action;
        
        private string[] _errors = null;

        public ActionNode(Orm.Merge.Action.MergeAction action) : base(action.ToString())
        {
            _action = action;
            ImageKey = action.ObjectType.ToString();
            SelectedImageKey = action.ObjectType.ToString();            
        }

        public int StartLine { get; set; }
        public int EndLine { get; set; }

        public Orm.Merge.Action.MergeAction Action { get { return _action; } }        

        public bool IsValid { get { return _errors == null || !_errors.Any(); } }

        public string[] ValidationErrors
        {
            get { return _errors; }
            set
            {
                if (value != _errors)
                {
                    _errors = value;
                    ForeColor = (IsValid) ? SystemColors.WindowText : Color.Red;
                }
            }
        }
    }
}
