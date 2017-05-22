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
    }
}
