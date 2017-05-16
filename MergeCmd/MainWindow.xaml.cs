using Microsoft.Win32;
using Postulate.Orm;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Merge;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
                Dictionary<MergeActionType, BitmapImage> actionTypeImages = new Dictionary<MergeActionType, BitmapImage>()
                {
                    { MergeActionType.Create, new BitmapImage(new Uri("pack://application:,,,/Img/ActionType/Create.png")) },
                    { MergeActionType.Alter, new BitmapImage(new Uri("pack://application:,,,/Img/ActionType/Alter.png")) },
                    { MergeActionType.Drop, new BitmapImage(new Uri("pack://application:,,,/Img/ActionType/Drop.png")) }
                };

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
                        foreach (var actionType in diffs.GroupBy(item => item.ActionType))
                        {
                            TreeViewItem tviActionType = new TreeViewItem() { Header = $"{actionType.Key} ({actionType.Count()})" };
                            tviDb.Items.Add(tviActionType);                            

                            foreach (var objectType in actionType.GroupBy(item => item.ObjectType))
                            {
                                TreeViewItem tviObjectType = new TreeViewItem() { Header = $"{objectType.Key} ({objectType.Count()})" };
                                tviActionType.Items.Add(tviObjectType);

                                foreach (var diff in objectType)
                                {
                                    TreeViewItem tviDiff = new TreeViewItem() { Header = diff.ToString() };
                                    tviObjectType.Items.Add(tviDiff);

                                    foreach (var cmd in diff.SqlCommands(cn))
                                    {
                                        TreeViewItem tviCmd = new TreeViewItem() { Header = cmd };
                                        tviDiff.Items.Add(tviCmd);
                                    }
                                }

                                tviObjectType.IsExpanded = true;
                            }

                            tviActionType.IsExpanded = true;
                        }
                    }

                    tviDb.IsExpanded = true;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }       
    }
}
