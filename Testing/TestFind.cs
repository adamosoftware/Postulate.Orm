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
    public class TestFind
    {
        [TestMethod]
        public void ExistsWhere()
        {
            var db = new PostulateDb();
            bool result = db.ExistsWhere<Organization>("[Id]>@id", new { id = 0 });
        }
    }

    
}
