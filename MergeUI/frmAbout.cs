using AdamOneilSoftware;
using AzDeploy.Client;
using AzDeploy.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    public partial class frmAbout : Form
    {
        private InstallManager _im = new InstallManager("adamosoftware", "install", "PostulateSchemaMergeSetup.exe", "PostulateSchemaMerge");

        public frmAbout()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Shell.ViewDocument(@"https://github.com/adamosoftware/Postulate.Orm/tree/master/MergeUI");
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private async void frmAbout_Load(object sender, EventArgs e)
        {
            try
            {
                lblCoreVersion.Text = $"Postulate.Orm.Core version {GetFileVersion("Postulate.Orm.Core.dll")}";
                lblUIVersion.Text = $"MergeUI version {GetFileVersion("MergeUI.exe")}";
                btnInstallUpdate.Visible = await _im.IsNewVersionAvailableAsync();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private string GetFileVersion(string fileName)
        {
            string folder = Path.GetDirectoryName(Application.ExecutablePath);
            string path = Path.Combine(folder, fileName);
            return FileVersionInfo.GetVersionInfo(path).FileVersion;
        }

        private async void btnInstallUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                pbMain.Visible = true;
                await _im.AutoInstallAsync();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
    }
}