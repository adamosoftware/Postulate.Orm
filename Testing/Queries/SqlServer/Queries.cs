using AdamOneilSoftware;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm;
using Postulate.Orm.ModelMerge.Actions;
using Postulate.Orm.Models;
using Postulate.Orm.SqlServer;
using Postulate.Orm.Util;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Testing.Models;
using Testing.Queries.SqlServer;

namespace Testing
{
	[TestClass]
	public class SqlServerQueries
	{
		private static SqlServerDb<int> _sqlDb = new SqlServerDb<int>("PostulateWebDemo", "traceUser");

		[TestInitialize]
		public void InitializeSqlServerDb()
		{
			_sqlDb.TraceCallback = ShowQueryInfo;
		}

		[TestMethod]
		public void SaveTrace()
		{
			var results = new AllOrgsWithDb(_sqlDb).Execute();
		}

		[TestMethod]
		public void AllOrgsWithoutDb()
		{
			var qry = new AllOrgs();
			var data = qry.Execute(_sqlDb);
			Assert.IsTrue(data.Any());
		}

		[TestMethod]
		public void AllOrgsOneParam()
		{
			var qry = new AllOrgsOneParam() { Name = "fred" };
			var data = qry.Execute(_sqlDb);
			Assert.IsTrue(qry.ResolvedSql.Equals("SELECT * FROM [dbo].[Organization] WHERE [Name] LIKE '%'+@name+'@'"));			
		}

		[TestMethod]
		public void PagedQuery()
		{
			var tdg = new TestDataGenerator();

			using (var cn = _sqlDb.GetConnection())
			{
				cn.Open();

				if (!_sqlDb.Syntax.TableExists(cn, typeof(Customer)))
				{
					new CreateTable(_sqlDb.Syntax, _sqlDb.Syntax.GetTableInfoFromType(typeof(Customer))).Execute(cn);
				}

				tdg.GenerateUpTo<Customer>(cn, 10000,
					(c) => c.QuerySingle<int>("SELECT COUNT(1) FROM [dbo].[Customer]"),
					(c) =>
					{
						c.FirstName = tdg.Random(Source.FirstName);
						c.LastName = tdg.Random(Source.LastName);
						c.Phone = tdg.Random(Source.USPhoneNumber);
					}, (records) =>
					{
						_sqlDb.SaveMultiple(records);
					});

				var qry = new Query<Customer>("SELECT * FROM [Customer] ORDER BY [LastName]", _sqlDb);

				for (int page = 1; page < 10; page++)
				{
					var results = qry.Execute(page);
					Assert.IsTrue(results.Count() == qry.RowsPerPage);
				}
			}
		}		

		[TestMethod]
		public void LikeSearchParams()
		{
			var qry = new CustomerSearch(_sqlDb) { OrgId = 1, Search = "hello" };
			var results = qry.Execute();
		}

		/*
        [TestMethod]
        public void QueryArrayParams()
        {
            var qry = new ProductAttributesDownload();
            qry.AttributeId = new int[] { 1844, 1843, 3, 18 };

            using (var cn = _db.GetConnection())
            {
                qry.CommandTimeout = 120;
                var results = qry.Execute(cn);
                Assert.IsTrue(results.Any());
            }
        }
        */

		private void TestSaveTrace(IDbConnection cn, QueryTrace trace)
		{
			QueryUtil.SaveTrace(cn, trace, _sqlDb);
		}

		private void ShowQueryInfo(IDbConnection cn, QueryTrace trace)
		{
			Debug.WriteLine($"{trace.QueryClass}\n{trace.UserName}\n{trace.Sql}\n{trace.GetParameterValueString()}\n{trace.Duration}ms");
		}
	}
}