using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Testing.Models;

namespace Testing.Queries.MySql
{
    public class AllCustomers : Query<Customer>
    {
        // in practical use, you would pass the Postulate.Mvc.BaseController.Db property to your queries                
        public AllCustomers(SqlDb<int> db) : base("SELECT * FROM `customer` {where}", db)
        {
        }
        
        [Where("`LastName` LIKE CONCAT('%', @lastName, '%')")]
        public string LastName { get; set; }

        [Where("`Email` LIKE CONCAT('%', @email, '%')")]
        public string Email { get; set; }

        [Where("`Phone` LIKE CONCAT('%', @phone, '%')")]
        public string Phone { get; set; }

        [Case(true, "`Phone` IS NOT NULL")]
        [Case(false, "`Phone` IS NULL")]
        public bool? HasPhone { get; set; }
    }
}