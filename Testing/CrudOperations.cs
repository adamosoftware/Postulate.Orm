using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testing.Models;
using System.Data;
using Dapper;

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
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                db.Save(cn, org);
                db.DeleteOne(cn, org);
            }
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
            var newOrg = db.CopyOne<Organization>(1, new { name = $"Org Copy {DateTime.Now.ToString()}", description = "copied record" });
            Assert.IsTrue(newOrg.Description.Equals("copied record"));
        }

        [TestMethod]
        public void CopyOrgOmitColumns()
        {
            var db = new PostulateDb();
            var newOrg = db.CopyOne<Organization>(1, 
                new { createdBy = "/system", dateCreated = DateTime.Now, name = $"Org Copy {DateTime.Now.ToString()}", description = "copied record" }, 
                "ModifiedBy", "DateModified");
            Assert.IsTrue(newOrg.Description.Equals("copied record"));
        }

        [TestMethod]
        public void DeleteWhere()
        {
            var db = new PostulateDb();
            db.DeleteAllWhere<Organization>("[Name] LIKE @name + '%'", new { name = "Org Copy" });
        }
    }
}
