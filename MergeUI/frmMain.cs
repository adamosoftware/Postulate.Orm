using Postulate.Orm;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void btnSelectAssembly_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Assemblies|*.dll;*.exe|All Files|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    tbAssembly.Text = dlg.FileName;
                    BuildTreeView(dlg.FileName);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void BuildTreeView(string fileName)
        {
            tvwActions.Nodes.Clear();

            var assembly = Assembly.LoadFile(fileName);
            var config = ConfigurationManager.OpenExeConfiguration(assembly.Location);

            var dbTypes = assembly.GetTypes().Where(t => t.IsDerivedFromGeneric(typeof(SqlServerDb<>)));
            foreach (var dbType in dbTypes)
            {
                Type schemaMergeBaseType = typeof(SchemaMerge<>);
                var schemaMergeGenericType = schemaMergeBaseType.MakeGenericType(dbType);
                var db = Activator.CreateInstance(dbType, config) as IDb;
                using (var cn = db.GetConnection())
                {
                    cn.Open();

                    DbNode dbNode = new DbNode(dbType.Name, cn);
                    tvwActions.Nodes.Add(dbNode);

                    var schemaMerge = Activator.CreateInstance(schemaMergeGenericType) as ISchemaMerge;
                    var diffs = schemaMerge.Compare(cn);
                    var script = schemaMerge.GetScript(cn, diffs);
                    tbSQL.Text = script.ToString();

                    foreach (var actionType in diffs.GroupBy(item => item.ActionType))
                    {
                        ActionTypeNode ndActionType = new ActionTypeNode(actionType.Key, actionType.Count());
                        dbNode.Nodes.Add(ndActionType);

                        foreach (var objectType in actionType.GroupBy(item => item.ObjectType))
                        {
                            ObjectTypeNode ndObjectType = new ObjectTypeNode(objectType.Key, objectType.Count());
                            ndActionType.Nodes.Add(ndObjectType);

                            foreach (var diff in objectType)
                            {
                                ActionNode ndAction = new ActionNode(objectType.Key, diff.ToString());
                                ndObjectType.Nodes.Add(ndAction);

                                /*foreach (var cmd in diff.SqlCommands(cn))
                                {
                                    TreeViewItem tviCmd = new TreeViewItem() { Header = cmd };
                                    tviDiff.Items.Add(tviCmd);
                                }*/
                            }

                            ndObjectType.Expand();
                        }

                        ndActionType.Expand();
                    }

                    dbNode.Expand();
                }                
            }     
        }

        private void frmMain_ResizeEnd(object sender, EventArgs e)
        {
            int width = this.Width - btnExecute.Width - toolStripLabel1.Width - btnSaveAs.Width - btnSelectAssembly.Width - 50;
            tbAssembly.Size = new Size(width, tbAssembly.Height);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            frmMain_ResizeEnd(sender, e);
        }
    }
}
