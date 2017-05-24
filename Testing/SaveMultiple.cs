using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.Exceptions;
using System;
using System.Linq;
using Testing.Models;

namespace Testing
{
    [TestClass]
    public class SaveMultiple
    {
        [TestMethod]
        public void TestSaveAsync()
        {
            var tableBItems = new TableB[]
            {
                new TableB() { Description = "whatever", OrganizationId = 1, CreatedBy = "adamo" },
                new TableB() { Description = "this thing", OrganizationId = 1, CreatedBy = "adamo" },
                new TableB() { Description = "circumscribed", OrganizationId = 1, CreatedBy = "adamo" },
                new TableB() { Description = "least confident", OrganizationId = 1, CreatedBy = "adamo" }
            };

            var db = new PostulateDb();
            db.SaveMultipleAsync(tableBItems).Wait();            
        }
    }
}
