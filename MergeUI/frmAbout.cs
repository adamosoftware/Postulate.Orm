using AdamOneilSoftware;
using System;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    public partial class frmAbout : Form
    {
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
    }
}