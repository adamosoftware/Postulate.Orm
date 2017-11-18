using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.MySql;
using System.Diagnostics;
using System.Linq;
using Testing.Queries.MySql;
using Postulate.Orm.Models;
using System;
using System.Data;

namespace Testing
{
    [TestClass]
    public class TestQuery
    {
        private static MySqlDb<int> _db = new MySqlDb<int>("MySql", "system");

        [TestMethod]
        public void AllCustomersNoParams()
        {
            var results = new AllCustomers(_db).Execute();
            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void AllCustomersForEmail()
        {
            var results = new AllCustomers(_db) { Email = "nowhere.org", TraceCallback = ShowQueryInfo }.Execute();

            foreach (var row in results) Debug.WriteLine(row.Email);

            Assert.IsTrue(results.Any());
        }

        private void ShowQueryInfo(IDbConnection cn, QueryTrace trace)
        {
            Debug.WriteLine($"{trace.Sql}\n{trace.GetParameterValueString()}\n{trace.Duration}ms");
        }
    }
}