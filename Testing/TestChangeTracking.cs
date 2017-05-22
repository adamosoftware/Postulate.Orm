using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing.Models;

namespace Testing
{
    [TestClass]
    public class TestChangeTracking
    {
        [TestMethod]
        public void TrackTableAChanges()
        {
            var db = new PostulateDb();
            TableA a = new TableA() { FirstName = "Julio", LastName = "Arragato" };

            db.DeleteWhere<TableA>("[FirstName]=@FirstName AND [LastName]=@LastName", a);
            db.DeleteWhere<TableA>("[FirstName]=@FirstName AND [LastName]=@LastName", new { FirstName = "Geoffrey", LastName = "Arragato" });

            db.Save(a);

            a.FirstName = "Geoffrey";
            db.Save(a);      

            var history = db.QueryChangeHistory<TableA>(a.Id);
            var changes = history.First().Properties.ToDictionary(item => item.PropertyName);
            Assert.IsTrue(changes["FirstName"].OldValue.Equals("Julio") && changes["FirstName"].NewValue.Equals("Geoffrey"));
        }

        [TestMethod]
        public void TrackTableBChanges()
        {
            var db = new PostulateDb();
            db.DeleteWhere<TableB>("[Description]='Whatever'", null);

            string oldName = db.Find<Organization>(1).Name;
            string newName = db.Find<Organization>(2).Name;

            TableB b = new TableB() { OrganizationId = 1, Description = "Whatever" };
            db.Save(b);

            b.OrganizationId = 2;
            db.Save(b);

            var history = db.QueryChangeHistory<TableB>(b.Id);
            var changes = history.First().Properties.ToDictionary(item => item.PropertyName);
            Assert.IsTrue(changes["OrganizationId"].OldValue.Equals(oldName) && changes["OrganizationId"].NewValue.Equals(newName));
        }
    }
}
