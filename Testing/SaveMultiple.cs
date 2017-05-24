using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                new TableB() { Description = "whatever", OrganizationId = 1 },
                new TableB() { Description = "this thing", OrganizationId = 1 },
                new TableB() { Description = "circumscribed", OrganizationId = 1 },
                new TableB() { Description = "least confident", OrganizationId = 1 }
            };

            var db = new PostulateDb();
            db.SaveAsync(tableBItems);
        }
    }
}
