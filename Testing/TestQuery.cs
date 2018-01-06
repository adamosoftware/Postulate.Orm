using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.MySql;
using System.Diagnostics;
using System.Linq;
using Testing.Queries.MySql;
using Postulate.Orm.Models;
using System;
using System.Data;
using Testing.Models;
using Testing.Queries.SqlServer;
using Postulate.Orm.SqlServer;
using Postulate.Orm.Abstract;
using Postulate.Orm.Util;
using AdamOneilSoftware;
using Dapper;
using Postulate.Orm.ModelMerge.Actions;
using Postulate.Orm;

namespace Testing
{
    [TestClass]
    public class TestQuery
    {
        private static MySqlDb<int> _db = new MySqlDb<int>("MySql", "system");
        private static SqlServerDb<int> _sqlDb = new SqlServerDb<int>("SchemaMergeTest", "traceUser");

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

        [TestMethod]
        public void SingleCustomerAsync()
        {
            int id = 0;
            Customer c = null;
            do
            {
                id++;
                c = new SingleCustomer(_db) { Id = id, TraceCallback = ShowQueryInfo }.ExecuteSingleAsync().Result;
            } while (c == null);

            Assert.IsTrue(c != null);

        }

        [TestMethod]
        public void SaveTrace()
        {            
            var results = new AllOrgs(_sqlDb) { TraceCallback = TestSaveTrace }.Execute();            
        }

        [TestMethod]
        public void PagedQuery()
        {
            var tdg = new TestDataGenerator();

            using (var cn = _sqlDb.GetConnection())
            {
                cn.Open();

                if (!_sqlDb.Syntax.TableExists(cn, typeof(Customer)))
                {
                    new CreateTable(_sqlDb.Syntax, _sqlDb.Syntax.GetTableInfoFromType(typeof(Customer))).Execute(cn);                    
                }

                tdg.GenerateUpTo<Customer>(cn, 10000,
                    (c) => c.QuerySingle<int>("SELECT COUNT(1) FROM [dbo].[Customer]"),
                    (c) =>
                    {
                        c.FirstName = tdg.Random(Source.FirstName);
                        c.LastName = tdg.Random(Source.LastName);
                        c.Phone = tdg.Random(Source.USPhoneNumber);
                    }, (records) =>
                    {
                        _sqlDb.SaveMultiple(records);
                    });

                var qry = new Query<Customer>("SELECT * FROM [Customer] ORDER BY [LastName]", _sqlDb);

                for (int page = 1; page < 10; page++)
                {
                    var results = qry.Execute(page);
                    Assert.IsTrue(results.Count() == qry.RowsPerPage);
                }                
            }         
        }

        private void TestSaveTrace(IDbConnection cn, QueryTrace trace)
        {
            Query.SaveTrace(cn, trace, _sqlDb);
        }

        private void ShowQueryInfo(IDbConnection cn, QueryTrace trace)
        {
            Debug.WriteLine($"{trace.QueryClass}\n{trace.UserName}\n{trace.Sql}\n{trace.GetParameterValueString()}\n{trace.Duration}ms");
        }
    }
}