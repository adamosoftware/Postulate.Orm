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
                DropTablesIfExists(cn, "TableA", "TableB");
            }
        }

        [TestMethod]
        public void DetectNewTables()
        {     
            var sm = new Postulate.Merge.SchemaMerge<PostulateDb>();

            var db = new PostulateDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                var diffs = sm.Compare(cn);

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
        }

        [TestMethod]
        public void CreateNewTables()
        {
            var sm = new Postulate.Merge.SchemaMerge<PostulateDb>();

            var db = new PostulateDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                var diffs = sm.Compare(cn);
            }
        }

        private void DropTablesIfExists(IDbConnection cn, params string[] tableNames)
        {
            foreach (var tbl in tableNames)
            {
                DbObject obj = DbObject.Parse(tbl);
                if (cn.TableExists(obj.Schema, obj.Name))
                {
                    cn.Execute($"DROP TABLE [{obj.Schema}].[{obj.Name}]");
                }
            }
        }
    }
}
