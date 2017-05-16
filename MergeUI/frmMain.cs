using Postulate.Orm;
using Postulate.Orm.Extensions;
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

namespace MergeUI
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

            }
        }
    }
}
