using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.Merge;
using Testing.Models;
using Postulate.Orm.SqlServer;
using System.Diagnostics;
using System.Linq;

namespace Testing
{
    [TestClass]
    public class TestSchemaMerge
    {
        [TestMethod]
        public void GenerateScript()
        {
            IProgress<MergeProgress> showProgress = new Progress<MergeProgress>(ReportProgress);
            var engine = new Engine<SqlServerSyntax>(new Type[]
            {
                typeof(TableA),
                typeof(TableB),
                typeof(TableC),
                typeof(Organization)
            }, showProgress);

            var db = new SqlServerDb<int>("SchemaMergeTest");
            db.CreateIfNotExists();

            using (var cn = db.GetConnection())
            {
                var actions = engine.CompareAsync(cn).Result;
                Assert.IsTrue(actions.Count() == 5);
            }
        }

        private void ReportProgress(MergeProgress obj)
        {
            Debug.WriteLine(obj.ToString());
        }
    }
}
