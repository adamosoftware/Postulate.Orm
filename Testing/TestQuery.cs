﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.MySql;
using System.Diagnostics;
using System.Linq;
using Testing.Queries.MySql;
using Postulate.Orm.Models;
using System;
using System.Data;
using Testing.Models;

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

        [TestMethod]
        public void AllCustomersCase()
        {
            var results = new AllCustomers(_db) { HasPhone = false, TraceCallback = ShowQueryInfo }.Execute();
            Assert.IsTrue(results.All(row => string.IsNullOrEmpty(row.Phone)));
        }

        [TestMethod]
        public void SingleCustomer()
        {
            // I don't want to hard-code an Id value, so I'm doing this crazy loop to keep searching until I find a record
            // the point is simply to find a single record with ExecuteSingle method

            int id = 0;
            Customer c = null;
            do
            {
                id++;
                c = new SingleCustomer(_db) { Id = id, TraceCallback = ShowQueryInfo }.ExecuteSingle();
            } while (c == null);

            Assert.IsTrue(c != null);
        }

        private void ShowQueryInfo(IDbConnection cn, QueryTrace trace)
        {
            Debug.WriteLine($"{trace.QueryClass}\n{trace.UserName}\n{trace.Sql}\n{trace.GetParameterValueString()}\n{trace.Duration}ms");
        }
    }
}