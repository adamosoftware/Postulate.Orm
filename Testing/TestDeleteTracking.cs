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
    public class TestDeleteTracking
    {
        [TestMethod]
        public void TrackDeletion()
        {
            PostulateDb db = new PostulateDb();
            TableB b = new TableB() { OrganizationId = 1, Description = "whatever" };
            db.Save(b);
            db.DeleteOne(b);
        }
    }
}
