using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.Exceptions;
using System;
using System.Data;
using System.Linq;
using Testing.Models;
using Dapper;

namespace Testing
{
    [TestClass]
    public class SaveMultiple
    {
        [TestMethod]
        public void TestSaveMultipleAsync()
        {
            PostulateDb db;
            TableB[] tableBItems;
            InitTestRecords(out db, out tableBItems);
            db.SaveMultipleAsync(tableBItems).Wait();
        }

        private static void InitTestRecords(out PostulateDb db, out TableB[] tableBItems)
        {
            db = new PostulateDb();
            db.GetConnection().Execute("DELETE [TableB] WHERE [Description] IN ('whatever', 'this thing', 'circumscribed', 'least confident')");

            tableBItems = new TableB[]
            {
                new TableB() { Description = "whatever", OrganizationId = 1, CreatedBy = "adamo" },
                new TableB() { Description = "this thing", OrganizationId = 1, CreatedBy = "adamo" },
                new TableB() { Description = "circumscribed", OrganizationId = 1, CreatedBy = "adamo" },
                new TableB() { Description = "least confident", OrganizationId = 1, CreatedBy = "adamo" }
            };
        }

        [TestMethod]
        public void TestSaveMultipleEach()
        {
            PostulateDb db;
            TableB[] tableBItems;
            InitTestRecords(out db, out tableBItems);
            db.SaveMultipleAsync(tableBItems, batchSize:1).Wait();
        }
    }
}
