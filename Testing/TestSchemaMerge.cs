using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.Merge;
using Postulate.Orm.Merge.Actions;
using Postulate.Orm.Models;
using Postulate.Orm.SqlServer;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Testing.Models;

namespace Testing
{
    [TestClass]
    public class TestSchemaMerge
    {
        [TestMethod]
        public void FindActions()
        {
            Type[] modelClasses = GetModelClasses();
            var engine = GetEngine(modelClasses);
            var db = GetDb();

            using (var cn = db.GetConnection())
            {
                DropTables(cn, engine.Syntax, modelClasses);

                var actions = engine.CompareAsync(cn).Result;
                Assert.IsTrue(actions.Count() == 5);
            }
        }

        private static SqlServerDb<int> GetDb()
        {
            var db = new SqlServerDb<int>("SchemaMergeTest");
            db.CreateIfNotExists();
            return db;
        }

        private Engine<SqlServerSyntax> GetEngine(Type[] modelClasses)
        {
            return new Engine<SqlServerSyntax>(modelClasses, new Progress<MergeProgress>(ReportProgress));
        }

        [TestMethod]
        public void GenerateScript()
        {
            Type[] modelClasses = GetModelClasses();
            var engine = GetEngine(modelClasses);
            var db = GetDb();

            using (var cn = db.GetConnection())
            {
                var actions = engine.CompareAsync(cn).Result;
                engine.SaveScript(@"C:\Users\Adam\Desktop\Postulate Schema Merge.txt", cn, actions);
            }
        }

        private static Type[] GetModelClasses()
        {
            return new Type[]
            {
                typeof(TableA),
                typeof(TableB),
                typeof(TableC),
                typeof(Organization)
            };
        }

        private void DropTables(IDbConnection cn, SqlServerSyntax syntax, Type[] modelClasses)
        {
            foreach (var tbl in modelClasses)
            {
                if (syntax.TableExists(cn, tbl))
                {
                    cn.Execute($"DELETE {syntax.GetTableName(tbl)}");
                    var drop = new DropTable(syntax, TableInfo.FromModelType(tbl));
                    foreach (var cmd in drop.SqlCommands(cn)) cn.Execute(cmd);
                }
            }
        }

        private void ReportProgress(MergeProgress obj)
        {
            Debug.WriteLine(obj.ToString());
        }
    }
}