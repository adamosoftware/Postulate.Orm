using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using Testing.Models;
using System.Data;
using Postulate.Extensions;
using Postulate.Merge;
using Dapper;
using System.Linq;

namespace Testing
{
    [TestClass]
    public class SchemaMergeTest
    {
        [TestInitialize]
        public void DropTestTables()
        {
            var db = new PostulateDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                DropTablesIfExists(cn, "TableA", "TableB", "TableC");
            }
        }

        private void DropTablesIfExists(IDbConnection cn, params string[] tableNames)
        {
            // note the tables must be in safe FK drop order

            foreach (var tbl in tableNames)
            {
                DbObject obj = DbObject.Parse(tbl);
                if (cn.TableExists(obj.Schema, obj.Name))
                {
                    cn.Execute($"DROP TABLE [{obj.Schema}].[{obj.Name}]");
                }
            }
        }

        private bool AllTablesExist(IDbConnection cn, params string[] tableNames)
        {
            return tableNames.All(item =>
            {
                DbObject obj = DbObject.Parse(item, cn);
                return cn.TableExists(obj.Schema, obj.Name);
            });
        }

        private bool AllColumnsExist(IDbConnection cn, string tableName, params string[] columnNames)
        {
            return columnNames.All(item =>
            {
                DbObject obj = DbObject.Parse(item);
                return cn.ColumnExists(obj.Schema, obj.Name, item);
            });
        }

        [TestMethod]
        public void DetectNewTables()
        {
            var sm = new SchemaMerge<PostulateDb>();
            var diffs = sm.Compare();

            Assert.IsTrue(
                diffs.Any(item1 =>
                    item1.ActionType == MergeActionType.Create &&
                    item1.ObjectType == MergeObjectType.Table &&
                    item1.Description.Contains("TableA") &&
                diffs.Any(item2 =>
                    item2.ActionType == MergeActionType.Create &&
                    item2.ObjectType == MergeObjectType.Table &&
                    item2.Description.Contains("TableB"))));
        }

        [TestMethod]
        public void CreateNewTables()
        {
            var sm = new SchemaMerge<PostulateDb>();

            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();
                sm.Execute(cn);
                Assert.IsTrue(AllTablesExist(cn, "TableA", "TableB", "TableC"));
            }
        }

        [TestMethod]
        public void CreateNewNonKeyColumns()
        {
            var sm = new SchemaMerge<PostulateDb>();
            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();
                sm.Execute(cn);

                cn.Execute("ALTER TABLE [dbo].[TableC] DROP COLUMN [SomeDate]");
                cn.Execute("ALTER TABLE [dbo].[TableC] DROP COLUMN [SomeDouble]");

                sm.Execute(cn);

                Assert.IsTrue(AllColumnsExist(cn, "TableC", "SomeDate", "SomeDouble"));
            }
        }
    }
}

