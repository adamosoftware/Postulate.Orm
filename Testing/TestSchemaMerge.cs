using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.ModelMerge;
using Postulate.Orm.ModelMerge.Actions;
using Postulate.Orm.Models;
using Postulate.Orm.SqlServer;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Testing.Models;
using TestModels.Models;

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
                var script = engine.GetScript(cn, actions).ToString();
                Assert.IsTrue(script.Equals(GetEmbeddedResource("Testing.Resources.SqlServer.GenerateScript.txt")));
            }
        }

        [TestMethod]
        public void AddColumns()
        {
            Type[] modelClasses = GetModelClasses();
            var engine = GetEngine(modelClasses);
            var db = GetDb();

            using (var cn = db.GetConnection())
            {
                DropTables(cn, engine.Syntax, modelClasses);
                engine.ExecuteAsync(cn).Wait();
            }
        }

        [TestMethod]
        public void TableASeedData()
        {
            var sd = new TableASeedData();
            sd.Generate(GetDb());
        }

		[TestMethod]
		public void NoIdentityTable()
		{
			var ct = new CreateTable(new SqlServerSyntax(), new TableInfo("TableD", "dbo", typeof(TableD)));
			using (var cn = GetDb().GetConnection())
			{
				StringBuilder sb = new StringBuilder();
				foreach (string sql in ct.SqlCommands(cn)) sb.Append(sql);
				Assert.IsTrue(sb.ToString().Equals(
@"CREATE TABLE [dbo].[TableD] (
	[Id] int NOT NULL,
	[FieldOne] nvarchar(max) NULL,
	CONSTRAINT [PK_TableD] PRIMARY KEY CLUSTERED ([Id])
)"));
			}			
		}

		[TestMethod]
		public void IdentityTable()
		{
			var ct = new CreateTable(new SqlServerSyntax(), new TableInfo("TableC", "dbo", typeof(TableC)));
			using (var cn = GetDb().GetConnection())
			{
				StringBuilder sb = new StringBuilder();
				foreach (string sql in ct.SqlCommands(cn)) sb.Append(sql);
				Assert.IsTrue(sb.ToString().Equals(
@"CREATE TABLE [dbo].[TableC] (
	[Id] int identity(1,1),
	[SomeValue] bigint NOT NULL,
	[SomeDate] datetime NOT NULL,
	[SomeDouble] float NOT NULL,
	[AnotherValue] int NOT NULL,
	[DateCreated] datetime NOT NULL,
	[CreatedBy] nvarchar(20) NOT NULL,
	[DateModified] datetime NULL,
	[ModifiedBy] nvarchar(20) NULL,
	CONSTRAINT [PK_TableC] PRIMARY KEY CLUSTERED ([Id])
)"));
			}
		}

		private static Type[] GetModelClasses()
        {
            return new Type[]
            {
                typeof(TableA),
                typeof(TableB),
                typeof(TableC),
                typeof(Organization),
                typeof(Customer)
            };
        }

        private void DropTables(IDbConnection cn, SqlServerSyntax syntax, Type[] modelClasses)
        {
            foreach (var tbl in modelClasses)
            {
                if (syntax.TableExists(cn, tbl))
                {
                    cn.Execute($"DELETE {syntax.GetTableName(tbl)}");
                    var obj = TableInfo.FromModelType(tbl, "dbo");
                    syntax.FindObjectId(cn, obj);
                    var drop = new DropTable(syntax, obj);
                    foreach (var cmd in drop.SqlCommands(cn)) cn.Execute(cmd);
                }
            }
        }

        private void ReportProgress(MergeProgress obj)
        {
            Debug.WriteLine(obj.ToString());
        }

        private static string GetEmbeddedResource(string name)
        {
            using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream(name))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}