using System;
using System.Windows.Forms;

namespace Postulate.MergeUI
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string startupFile = ((args?.Length ?? 0) == 1) ? args[0] : null;
            Application.Run(new frmMain() { StartupFile = startupFile });
        }
    }
}