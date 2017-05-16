using Microsoft.Win32;
using Postulate.Orm;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge;
using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Postulate.MergeUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSelectAssembly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Assemblies|*.dll;*.exe|All Files|*.*";
                if (dlg.ShowDialog() ?? false)
                {
                    tbAssembly.Text = dlg.FileName;
                    Analyze(dlg.FileName);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void Analyze(string fileName)
        {
            try
            {
                var assembly = Assembly.LoadFile(fileName);
                var config = ConfigurationManager.OpenExeConfiguration(assembly.Location);

                var dbTypes = assembly.GetTypes().Where(t => t.IsDerivedFromGeneric(typeof(SqlServerDb<>)));
                foreach (var dbType in dbTypes)
                {
                    TreeViewItem tviDb = new TreeViewItem();
                    tviDb.Header = dbType.Name;
                    tvwMerge.Items.Add(tviDb);

                    Type schemaMergeBaseType = typeof(SchemaMerge<>);
                    var schemaMergeGenericType = schemaMergeBaseType.MakeGenericType(dbType);
                   
                    var db = Activator.CreateInstance(dbType, config) as IDb;
                    using (var cn = db.GetConnection())
                    {
                        cn.Open();
                        var schemaMerge = Activator.CreateInstance(schemaMergeGenericType) as ISchemaMerge;
                        var diffs = schemaMerge.Compare(cn);
                        foreach (var diff in diffs.GroupBy(item => item.ActionType))
                        {
                            TreeViewItem tviDiff = new TreeViewItem() { Header = diff.Key };
                            tviDb.Items.Add(tviDiff);
                        }
                    }                    
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void btnSelectAssembly_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
