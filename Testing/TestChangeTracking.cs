using Dapper;
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

            db.DeleteOneWhere<TableA>("[FirstName]=@FirstName AND [LastName]=@LastName", a);
            db.DeleteOneWhere<TableA>("[FirstName]=@FirstName AND [LastName]=@LastName", new { FirstName = "Geoffrey", LastName = "Arragato" });

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
            db.DeleteOneWhere<TableB>("[Description]='Whatever'", null);

            int[] orgIds = null;
            using (var cn = db.GetConnection())
            {
                cn.Open();
                orgIds = cn.Query<int>("SELECT [Id] FROM [Organization]").ToArray();
            }
                
            string oldName = db.Find<Organization>(orgIds[0]).Name;
            string newName = db.Find<Organization>(orgIds[1]).Name;

            TableB b = new TableB() { OrganizationId = 1, Description = "Whatever" };
            db.Save(b);

            b.OrganizationId = orgIds[1];
            db.Save(b);

            var history = db.QueryChangeHistory<TableB>(b.Id);
            var changes = history.First().Properties.ToDictionary(item => item.PropertyName);
            Assert.IsTrue(changes["OrganizationId"].OldValue.Equals(oldName) && changes["OrganizationId"].NewValue.Equals(newName));
        }

        [TestMethod]
        public void GetRecordVersion()
        {
            var db = new PostulateDb();
            db.DeleteOneWhere<TableB>("[Description]='Yadda Yadda'", null);

            int[] orgIds;
            using (var cn = db.GetConnection())
            {
                cn.Open();
                orgIds = cn.Query<int>("SELECT [Id] FROM [Organization]").ToArray();
            }
                
            var itemB = new TableB() { Description = "Yadda Yadda", OrganizationId = orgIds[0] };
            db.Save(itemB);

            itemB.OrganizationId = orgIds[1];
            db.Save(itemB);

            int version;
            db.Find<TableB>(itemB.Id, out version);
            Assert.IsTrue(version == 2);
        }
    }
}
