using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm;
using Postulate.Orm.Enums;
using Postulate.Orm.Util;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Testing.Models;

namespace Testing
{
	[TestClass]
	public class CrudOperations
	{
		private int MaxOrgId()
		{
			int result = 0;
			using (IDbConnection cn = new PostulateDb().GetConnection())
			{
				result = cn.QuerySingleOrDefault<int?>("SELECT MAX([Id]) FROM [Organization]") ?? 0;
			}
			return result;
		}

		[TestMethod]
		public void CreateDeleteOrg()
		{
			Organization org = new Organization();
			org.Name = DefaultOrgName();
			org.BillingRate = 10;

			PostulateDb db = new PostulateDb();
			db.TraceCallback = (cn, qt) => { QueryUtil.SaveTrace(cn, qt, db); };

			using (IDbConnection cn = db.GetConnection())
			{
				cn.Open();
				db.Save(cn, org);
				db.DeleteOne(cn, org);
			}
		}

		[TestMethod]
		public async Task CreateOrgAsync()
		{
			var org = new Organization() { Name = "Async Org", BillingRate = 1 };
			PostulateDb db = new PostulateDb();
			await db.SaveAsync(org);
			Assert.IsTrue(org.Id != 0);
			db.DeleteOne(org);
		}

		private string DefaultOrgName()
		{
			return $"Sample Org {DateTime.Now} {MaxOrgId()}";
		}

		[TestMethod]
		public void FindAndUpdateOrg()
		{
			Organization org = new Organization();
			org.Name = DefaultOrgName();
			org.BillingRate = 10;

			PostulateDb db = new PostulateDb();
			using (IDbConnection cn = db.GetConnection())
			{
				cn.Open();
				db.Save(cn, org);

				org = db.Find<Organization>(cn, org.Id);
				org.Name = $"Another Sample {DateTime.Now}";
				org.BillingRate = 11;
				db.Save(cn, org);
			}
		}

		[TestMethod]
		public void CreateDeleteOrgFromIdNoConnection()
		{
			Organization org = new Organization();
			org.Name = DefaultOrgName();
			org.BillingRate = 10;

			PostulateDb db = new PostulateDb();
			db.Save(org);
			db.DeleteOne<Organization>(org.Id);
		}

		[TestMethod]
		public void CreateDeleteOrgFromRecordNoConnection()
		{
			Organization org = new Organization();
			org.Name = DefaultOrgName();
			org.BillingRate = 10;

			PostulateDb db = new PostulateDb();
			db.Save(org);
			db.DeleteOne(org);
		}

		[TestMethod]
		public void Update()
		{
			Organization org = new Organization();
			org.Name = DefaultOrgName();
			org.BillingRate = 10;

			var db = new PostulateDb();
			db.Save(org);
			int orgId = org.Id;

			org.BillingRate = 11;
			db.Update(org, r => r.BillingRate);

			org = db.Find<Organization>(orgId);
			db.DeleteOne<Organization>(orgId);

			Assert.IsTrue(org.BillingRate == 11);
		}

		[TestMethod]
		public void UpdateSetDateModified()
		{
			Organization org = new Organization();
			org.Name = DefaultOrgName();
			org.BillingRate = 10;

			var db = new PostulateDb();
			db.Save(org);

			int orgId = org.Id;

			org.BillingRate = 11;
			db.Update(org, r => r.BillingRate);

			org = db.Find<Organization>(orgId);
			db.DeleteOne<Organization>(orgId);

			Assert.IsTrue(org.DateModified != null && org.ModifiedBy != null);
		}

		[TestMethod]
		public void CopyOrg()
		{
			var db = new PostulateDb();
			var srcOrg = FirstRandomOrg(db);
			var newOrg = db.Copy<Organization>(srcOrg.Id, new { name = $"Org Copy {DateTime.Now.ToString()}", description = "copied record" });
			Assert.IsTrue(newOrg.Description.Equals("copied record"));
		}

		[TestMethod]
		public void CopyOrgOmitColumns()
		{
			var db = new PostulateDb();

			Organization srcOrg = FirstRandomOrg(db);

			Random rnd = new Random();

			var newOrg = db.Copy<Organization>(srcOrg.Id,
				new { createdBy = "/system", dateCreated = DateTime.Now, name = $"Org Copy {rnd.Next(1000)}", description = "copied record" },
				"ModifiedBy", "DateModified");

			Assert.IsTrue(newOrg.Description.Equals("copied record"));
		}

		private static Organization FirstRandomOrg(PostulateDb db)
		{
			Organization srcOrg = null;
			var qry = new Query<Organization>("SELECT TOP (1) * FROM [dbo].[Organization]");

			while (srcOrg == null)
			{
				srcOrg = qry.ExecuteSingle(db);
				if (srcOrg != null) break;
				srcOrg = new Organization() { Name = "Sample", BillingRate = 10 };
				db.Save(srcOrg);
			}

			return srcOrg;
		}

		[TestMethod]
		public void DeleteWhere()
		{
			var db = new PostulateDb();
			db.DeleteAllWhere<Organization>("[Name] LIKE @name + '%'", new { name = "Org Copy" });
		}

		[TestMethod]
		public void SchemaAttribute()
		{
			//string tableName = DbObject.SqlServerName(typeof(TableD));
			//Assert.IsTrue(tableName.Equals("[app].[TableD]"));
		}

		[TestMethod]
		public void ValidateMissingPKValue()
		{
			var a = new TableA();
			a.FirstName = "whatever";
			using (var cn = new PostulateDb().GetConnection())
			{
				cn.Open();
				var errs = a.GetValidationErrors(cn, SaveAction.Insert);
				Assert.IsTrue(errs.Any(item => item.Equals("Primary key field LastName requires a value.")));
			}
		}
	}
}