using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testing.Models;
using System.Data.SqlClient;
using System.Data;

namespace Testing
{
    [TestClass]
    public class CrudOperations
    {
        [TestMethod]
        public void CreateDeleteOrg()
        {
            Organization org = new Organization();
            org.Name = $"Sample Org {DateTime.Now}";
            org.BillingRate = 10;

            PostulateDb db = new PostulateDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                db.Save(cn, org);
                db.Delete(cn, org);
            }
        }

        [TestMethod]
        public void FindAndUpdateOrg()
        {
            Organization org = new Organization();
            org.Name = $"Sample Org {DateTime.Now}";
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
    }
}
