using AdamOneilSoftware;
using FastColoredTextBoxNS;
using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        internal string AssemblyFilename { get; set; }
        internal Dictionary<string, MergeInfo> MergeActions { get; set; }

        private void btnSelectAssembly_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Assemblies|*.dll;*.exe|All Files|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    tbAssembly.Text = dlg.FileName;
                    MergeActions = Program.GetMergeActions(dlg.FileName);
                    BuildTreeView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void BuildTreeView()
        {
            tvwActions.Nodes.Clear();

            foreach (var key in MergeActions.Keys)
            {
                var mergeInfo = MergeActions[key];
                DbNode dbNode = new DbNode(key, mergeInfo.ServerAndDatabase);
                tvwActions.Nodes.Add(dbNode);

                tbSQL.Text = mergeInfo.Script.ToString();

                foreach (var actionType in mergeInfo.Actions.GroupBy(item => item.ActionType))
                {
                    ActionTypeNode ndActionType = new ActionTypeNode(actionType.Key, actionType.Count());
                    dbNode.Nodes.Add(ndActionType);

                    foreach (var objectType in actionType.GroupBy(item => item.ObjectType))
                    {
                        ObjectTypeNode ndObjectType = new ObjectTypeNode(objectType.Key, objectType.Count());
                        ndActionType.Nodes.Add(ndObjectType);

                        foreach (var action in objectType)
                        {
                            ActionNode ndAction = new ActionNode(action);
                            ndAction.StartLine = mergeInfo.LineRanges[action].Start;
                            ndAction.EndLine = mergeInfo.LineRanges[action].End;
                            ndObjectType.Nodes.Add(ndAction);
                        }

                        ndObjectType.Expand();
                    }

                    ndActionType.Expand();
                }

                dbNode.Expand();
                dbNode.Checked = true;
            }
        }        

        private void frmMain_ResizeEnd(object sender, EventArgs e)
        {
            int width = this.Width - btnExecute.Width - toolStripLabel1.Width - btnSaveAs.Width - btnSelectAssembly.Width - 50;
            tbAssembly.Size = new Size(width, tbAssembly.Height);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                frmMain_ResizeEnd(sender, e);

                if (MergeActions != null)
                {
                    tbAssembly.Text = AssemblyFilename;
                    BuildTreeView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }            
        }

        private void tvwActions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                ActionNode nd = e.Node as ActionNode;
                if (nd != null) tbSQL.Selection = new Range(tbSQL, new Place(0, nd.StartLine), new Place(0, nd.EndLine));
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "SQL Files|*.sql|All Files|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    tbSQL.SaveToFile(dlg.FileName, Encoding.ASCII);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void tvwActions_AfterCheck(object sender, TreeViewEventArgs e)
        {
            try
            {
                tvwActions.AfterCheck -= tvwActions_AfterCheck;
                e.Node.CheckChildNodes(e.Node.Checked);
                tvwActions.AfterCheck += tvwActions_AfterCheck;

                DbNode nd = e.Node.FindParentNode<DbNode>();
                if (nd == null) nd = tvwActions.Nodes[0] as DbNode;
                var mergeInfo = MergeActions[nd.ConnectionName];
                var selectedActions = tvwActions.FindNodes<ActionNode>(true, node => node.Checked);

                using (var cn = mergeInfo.Db.GetConnection())
                {
                    cn.Open();
                    Dictionary<Orm.Merge.Action.MergeAction, LineRange> lineRanges;
                    tbSQL.Text = mergeInfo.Merge.GetScript(cn, selectedActions.Select(node => node.Action), out lineRanges).ToString();
                    foreach (var actionNode in selectedActions)
                    {
                        actionNode.StartLine = lineRanges[actionNode.Action].Start;
                        actionNode.EndLine = lineRanges[actionNode.Action].End;
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }            
        }
    }
}
