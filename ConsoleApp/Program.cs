using ConsoleApp.Models;
using Dapper;
using System;
using Testing.Models;

namespace ConsoleApp
{
    internal class Program
    {
        private static SampleMySqlDb _db = new SampleMySqlDb();

        private static void Main(string[] args)
        {
            Customer c = _db.FindWhere<Customer>("`Email` IS NOT NULL", null);
            Console.WriteLine(c);
            Console.ReadLine();
        }

        private static void Main2(string[] args)
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