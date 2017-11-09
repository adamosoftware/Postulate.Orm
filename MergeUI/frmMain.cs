using AdamOneilSoftware;
using Postulate.MergeUI.ViewModels;
using Postulate.Orm.Merge;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    public partial class frmMain : Form
    {
        private ScriptManager _scriptManager;

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            try
            {
                SelectAssembly();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private bool SelectAssembly()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Assemblies|*.exe;*.dll|All Files|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tbAssembly.Text = dlg.FileName;
                _scriptManager = ScriptManager.FromFile(dlg.FileName);
                tvwActions.Nodes.Clear();
                this.Text = $"Postulate Schema Merge - {_scriptManager.CurrentSyntax.ToString()}";
                foreach (string name in _scriptManager.ConnectionNames) tvwActions.Nodes.Add(new ConnectionNode(name));
                return true;
            }

            return false;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            btnSelectFile_Click(sender, e);
        }

        private async void tvwActions_AfterExpand(object sender, TreeViewEventArgs e)
        {
            try
            {
                ConnectionNode cnNode = e.Node as ConnectionNode;
                if (cnNode != null) await BuildViewAsync(cnNode);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private async Task BuildViewAsync(ConnectionNode connectionNode)
        {
            connectionNode.Nodes.Clear();

            try
            {
                pbMain.Visible = true;
                await _scriptManager.GenerateScriptAsync(connectionNode.ConnectionName, new Progress<MergeProgress>(ShowProgress));                

                if (!_scriptManager.Actions.Any())
                {
                    btnExecute.Enabled = false;
                    tbScript.Text = $"{_scriptManager.Syntax.CommentPrefix} Nothing to execute -- no model differences were found.";
                    return;
                }

                tbScript.Text = _scriptManager.Script.ToString();

                foreach (var actionTypeGrp in _scriptManager.Actions.GroupBy(item => item.ActionType))
                {
                    ActionTypeNode actionTypeNode = new ActionTypeNode(actionTypeGrp.Key, actionTypeGrp.Count());
                    connectionNode.Nodes.Add(actionTypeNode);

                    foreach (var objectTypeGrp in actionTypeGrp.GroupBy(item => item.ObjectType))
                    {
                        ObjectTypeNode objectTypeNode = new ObjectTypeNode(objectTypeGrp.Key, objectTypeGrp.Count());
                        actionTypeNode.Nodes.Add(objectTypeNode);

                        foreach (var action in objectTypeGrp)
                        {
                            ActionNode ndAction = new ActionNode(action);
                            ndAction.StartLine = _scriptManager.LineRanges[action].Start;
                            ndAction.EndLine = _scriptManager.LineRanges[action].End;
                            ndAction.ValidationErrors = _scriptManager.ValidationErrors[action].ToArray();
                            objectTypeNode.Nodes.Add(ndAction);
                        }

                        objectTypeNode.Expand();
                    }
                    actionTypeNode.Expand();
                }
            }
            finally
            {
                pbMain.Visible = false;
                tslStatus.Text = "Ready";
            }
        }

        private void ShowProgress(MergeProgress obj)
        {
            tslStatus.Text = obj.Description;
            pbMain.Value = obj.PercentComplete;
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectionNode cnNode = tvwActions.SelectedNode?.FindParentNode<ConnectionNode>();
                if (cnNode != null) await BuildViewAsync(cnNode);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
    }
}