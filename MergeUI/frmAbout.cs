using AdamOneilSoftware;
using AzDeploy.Client;
using System;
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
                btnInstallUpdate.Visible = await _im.IsNewVersionAvailableAsync();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
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