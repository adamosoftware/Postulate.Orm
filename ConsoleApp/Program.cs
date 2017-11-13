using ConsoleApp.Models;
using Postulate;
using Postulate.Orm.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing.Models;
using Dapper;

namespace ConsoleApp
{
    class Program
    {
        private static SampleMySqlDb _db = new SampleMySqlDb();

        static void Main(string[] args)
        {
            var newCustomer = new Customer() { LastName = "Higgenbotham", FirstName = "Jupsider", Email = "whatever" };
            _db.Save(newCustomer);

            newCustomer.EffectiveDate = new DateTime(1983, 1, 1);
            _db.Update(newCustomer, r => r.EffectiveDate);

            using (var cn = _db.GetConnection())
            {
                cn.Open();

                var customerIds = cn.Query<int>("SELECT `Id` FROM `customer` ORDER BY `Id` DESC LIMIT 5");

                foreach (var id in customerIds)
                {
                    var c = _db.Find<Customer>(cn, id);
                    Console.WriteLine(c.ToString());
                }
                Console.ReadLine();
            }

        }
    }
}
