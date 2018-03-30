using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.MySql;
using Testing.Models;
using Testing.Queries.MySql;

namespace Testing
{
	[TestClass]
	public class MySqlQueries
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
			var results = new AllCustomers(_db) { Email = "nowhere.org" }.Execute();

			foreach (var row in results) Debug.WriteLine(row.Email);

			Assert.IsTrue(results.Any());
		}

		[TestMethod]
		public void AllCustomersCase()
		{
			var results = new AllCustomers(_db) { HasPhone = false }.Execute();
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
				c = new SingleCustomer(_db) { Id = id }.ExecuteSingle();
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
				c = new SingleCustomer(_db) { Id = id }.ExecuteSingleAsync().Result;
			} while (c == null);

			Assert.IsTrue(c != null);
		}
	}
}
