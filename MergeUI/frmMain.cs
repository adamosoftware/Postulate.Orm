using AdamOneilSoftware;
using FastColoredTextBoxNS;
using Newtonsoft.Json;
using Postulate.MergeUI.Models;
using Postulate.MergeUI.ViewModels;
using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.IO;
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

        public string StartupFile { get; set; }

        private async void btnSelectFile_Click(object sender, EventArgs e)
        {
            try
            {
                await SelectAssemblyAsync();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private async Task<bool> SelectAssemblyAsync()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Assemblies|*.exe;*.dll|All Files|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                await SelectAssemblyInner(dlg.FileName);
                return true;
            }

            return false;
        }

        private async Task SelectAssemblyInner(string fileName)
        {
            tbAssembly.Text = fileName;
            _scriptManager = ScriptManager.FromFile(fileName);
            tvwActions.Nodes.Clear();
            this.Text = $"Postulate Schema Merge - {_scriptManager.CurrentSyntax.ToString()}";
            foreach (string name in _scriptManager.ConnectionNames)
            {
                ConnectionNode cnNode = new ConnectionNode(name);
                tvwActions.Nodes.Add(cnNode);
                await BuildViewAsync(cnNode);
            }
        }

        private async void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                splcActions.Panel2Collapsed = true;
                tslSelectAssembly.Visible = false;

                if (!string.IsNullOrEmpty(StartupFile))
                {
                    await PromptAndLoadAssemblyAsync();
                }
                else
                {
                    btnSelectFile_Click(sender, e);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private async Task PromptAndLoadAssemblyAsync()
        {
            string loadFile = null;

            if (Directory.Exists(StartupFile))
            {
                tslSelectAssembly.Visible = true;
                loadFile = FindModelAssembly(StartupFile);
            }
            else if (File.Exists(StartupFile))
            {
                string ext = Path.GetExtension(StartupFile);
                tslSelectAssembly.Visible = (ext.Equals(".sln"));

                switch (ext)
                {
                    case ".dll":
                        tslSelectAssembly.Visible = false;
                        loadFile = StartupFile;
                        break;

                    case ".sln":
                    case ".csproj":
                        loadFile = FindModelAssembly(Path.GetDirectoryName(StartupFile));
                        tslSelectAssembly.Visible = true;
                        tslSelectAssembly.Text = $"Assembly in solution {Path.GetFileName(loadFile)}";
                        break;

                    default:
                        throw new ArgumentException($"The extension {ext} is not supported.");
                }
            }

            await SelectAssemblyInner(loadFile);
        }

        /// <summary>
        /// Finds a .json settings file in the given path that indicates the model assembly to use for schema merge. Prompts
        /// the user with File Open dialog if settings file isn't found, and saves results in new settings file
        /// </summary>        
        /// <param name="solutionFolder">Directory where solution is</param>
        /// <returns>Full path of model assembly</returns>
        private string FindModelAssembly(string solutionFolder)
        {        
            string settingsFile = SolutionSettingsFile(solutionFolder);
            SolutionSettings settings = null;            

            if (File.Exists(settingsFile))
            {
                settings = ReadJsonFile<SolutionSettings>(settingsFile);
                if (File.Exists(settings.ModelAssembly)) return settings.ModelAssembly;
            }

            settings = new SolutionSettings();
            settings.ModelAssembly = DialogSelectModelAssembly(solutionFolder);
            WriteJsonFile(settingsFile, settings);                        

            return settings.ModelAssembly;
        }

        private static string SolutionSettingsFile(string folder)
        {
            return Path.Combine(folder, "Postulate.MergeUI.Settings.json");
        }        

        private string DialogSelectModelAssembly(string folder)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Model Assembly";
            dlg.Filter = "Assemblies|*.exe;*.dll|All Files|*.*";
            dlg.InitialDirectory = Path.GetDirectoryName(folder);
            if (dlg.ShowDialog() == DialogResult.OK) return dlg.FileName;

            throw new Exception("User canceled model assembly selection.");
        }

        private void WriteJsonFile<T>(string fileName, T @object) where T : new()
        {
            string folder = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (var stream = File.CreateText(fileName))
            {
                string json = JsonConvert.SerializeObject(@object);
                stream.Write(json);
            }
        }

        private T ReadJsonFile<T>(string fileName) where T : new()
        {           
            using (var stream = File.OpenRead(fileName))
            {
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(json);
                }
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
                    }                    
                }

                connectionNode.Checked = true;
                connectionNode.ExpandAll();
            }
            finally
            {
                pbMain.Visible = false;
                string status = "Ready";
                if (_scriptManager.Stopwatch != null) status += $", ran in {_scriptManager.Stopwatch.ElapsedMilliseconds:n0}ms";
                tslStatus.Text = status;
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

        private void tvwActions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                splcActions.Panel2Collapsed = true;

                ActionNode nd = e.Node as ActionNode;
                if (nd?.Checked ?? false) tbScript.Selection = new Range(tbScript, new Place(0, nd.StartLine), new Place(0, nd.EndLine));

                if (!nd?.IsValid ?? false)
                {
                    splcActions.Panel2Collapsed = false;
                    lblErrors.Text = string.Join("\r\n", nd.ValidationErrors);
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

                ConnectionNode nd = e.Node.FindParentNode<ConnectionNode>();
                if (nd == null) nd = e.Node as ConnectionNode;
                var selectedActions = tvwActions.FindNodes<ActionNode>(true, node => node.Checked);
                
                Dictionary<Orm.Merge.MergeAction, LineRange> lineRanges;
                tbScript.Text = _scriptManager.ScriptSelectedActions(nd.ConnectionName, selectedActions.Select(node => node.Action), out lineRanges);                    
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private async void btnExecute_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectionNode nd = tvwActions.SelectedNode.FindParentNode<ConnectionNode>();
                if (nd == null) nd = tvwActions.SelectedNode as ConnectionNode;
                if (nd == null) throw new Exception("Couldn't find connection node");

                var selectedActions = nd.FindNodes<ActionNode>(true, node => node.Checked);
                await _scriptManager.ExecuteSelectedActionsAsync(nd.ConnectionName, selectedActions.Select(node => node.Action), new Progress<MergeProgress>(ShowProgress));

                await BuildViewAsync(nd);

                string message = "Changes executed successfully!";
                bool exit = false;
                if (!tvwActions.FindNodes<ActionNode>().Any())
                {
                    message += " Schema Merge will exit since there are no more changes.";
                    exit = true;
                }
                MessageBox.Show(message);

                if (exit) Application.Exit();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void tslAbout_Click(object sender, EventArgs e)
        {
            frmAbout dlg = new frmAbout();
            dlg.ShowDialog();
        }

        private async void tslSelectAssembly_Click(object sender, EventArgs e)
        {
            try
            {
                string folder = Path.GetDirectoryName(StartupFile);
                string settingsFile = SolutionSettingsFile(folder);
                if (File.Exists(settingsFile)) File.Delete(settingsFile);
                await PromptAndLoadAssemblyAsync();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
    }
}