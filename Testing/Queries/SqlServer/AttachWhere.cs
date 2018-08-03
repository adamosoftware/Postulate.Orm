using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm;
using Postulate.Orm.Attributes;
using Postulate.Orm.Interfaces;
using Postulate.Orm.SqlServer;
using Testing.Models;

namespace Testing.Queries.SqlServer
{
	[TestClass]
	public class AttachWhere
	{
		private static SqlServerDb<int> _sqlDb = new SqlServerDb<int>("SchemaMergeTest", "traceUser");

		[TestMethod]
		public void TestAttachWhere()
		{
			var qry = new TestAttachWhereQuery(_sqlDb);
			qry.FirstName = "Tom%";
			qry.Additional.Description = "whatever";

			var results = qry.Execute();

			Assert.IsTrue(qry.ResolvedSql.Equals("SELECT * FROM [TableA] WHERE [FirstName] LIKE @firstName AND [Description] LIKE @description"));			
		}
	}

	public class TestAttachWhereQuery : Query<TableA>
	{
		public TestAttachWhereQuery(ISqlDb db) : base("SELECT * FROM [TableA] {where}", db)
		{
		}

		[Where("[FirstName] LIKE @firstName")]
		public string FirstName { get; set; }

		[Where("[LastName] LIKE @lastName")]
		public string LastName { get; set; }

		[AttachWhere]
		public AttachWhereCriteria Additional { get; set; } = new AttachWhereCriteria();
	}

	public class AttachWhereCriteria
	{
		[Where("[Description] LIKE @description")]
		public string Description { get; set; }
	}
}