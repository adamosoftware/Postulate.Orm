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
        private Options _options = null;

        public frmMain()
        {
            InitializeComponent();
        }

        internal string AssemblyFilename { get; set; }
        internal Dictionary<string, MergeViewModel> MergeActions { get; set; }

        private void btnSelectAssembly_Click(object sender, EventArgs e)
        {
            try
            {
                OpenAssembly();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OpenAssembly()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Assemblies|*.dll;*.exe|All Files|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tbAssembly.Text = dlg.FileName;
                Refresh();
            }
        }

        private void BuildTreeView()
        {
            tvwActions.Nodes.Clear();

            foreach (var key in MergeActions.Keys)
            {
                var vm = MergeActions[key];
                DbNode dbNode = new DbNode(key, vm.ServerAndDatabase);
                tvwActions.Nodes.Add(dbNode);

                tbSQL.Text = vm.Script.ToString();

                using (var cn = vm.Db.GetConnection())
                {
                    cn.Open();
                    foreach (var actionType in vm.Actions.GroupBy(item => item.ActionType))
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
                                ndAction.StartLine = vm.LineRanges[action].Start;
                                ndAction.EndLine = vm.LineRanges[action].End;                                
                                ndAction.ValidationErrors = action.ValidationErrors(cn).ToArray();
                                ndObjectType.Nodes.Add(ndAction);
                            }

                            ndObjectType.Expand();
                        }

                        ndActionType.Expand();
                    }
                }                    

                dbNode.Expand();
                dbNode.Checked = true;
            }
        }        

        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                Text = $"Postulate Schema Merge - {Application.ProductVersion}";

                _options = UserOptionsBase.Load<Options>("Options.xml", this);
                _options.TrackFormPosition(this, (fp) => _options.MainFormPosition = fp);
                _options.RestoreFormPosition(_options.MainFormPosition, this);
                if (_options.SplitterPosition > 0) splitContainer1.SplitterDistance = _options.SplitterPosition;

                splcActions.Panel2Collapsed = true;

                if (MergeActions != null)
                {
                    tbAssembly.Text = AssemblyFilename;
                    BuildTreeView();
                }
                else
                {
                    OpenAssembly();
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
                if (nd?.Checked ?? false) tbSQL.Selection = new Range(tbSQL, new Place(0, nd.StartLine), new Place(0, nd.EndLine));

                if (!nd?.IsValid ?? false)
                {
                    splcActions.Panel2Collapsed = false;
                    lblErrors.Text = string.Join("\r\n", nd.ValidationErrors);
                }
                else
                {
                    splcActions.Panel2Collapsed = true;
                }
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

        private void btnExecute_Click(object sender, EventArgs e)
        {
            try
            {
                DbNode nd = tvwActions.SelectedNode?.FindParentNode<DbNode>();
                if (nd == null) nd = tvwActions.Nodes[0] as DbNode;

                var mergeInfo = MergeActions[nd.ConnectionName];
                var selectedActions = tvwActions.FindNodes<ActionNode>(true, node => node.Checked);

                using (var cn = mergeInfo.Db.GetConnection())
                {
                    cn.Open();
                    mergeInfo.Merge.Execute(cn, selectedActions.Select(node => node.Action));                    
                }

                MessageBox.Show("Changes executed successfully!");

                Refresh();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        
        public new void Refresh()
        {
            base.Refresh();
            MergeActions = Program.GetMergeActions(tbAssembly.Text);
            BuildTreeView();
        }

        private void tsbRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                Refresh();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            _options.SplitterPosition = e.SplitX;
        }
    }
}
