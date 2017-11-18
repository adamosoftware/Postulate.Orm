using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Testing.Models;

namespace Testing.Queries.MySql
{
    public class AllCustomers : Query<Customer>
    {
        // in practical use, you would not pass a db in your query constructor, 
        // but rather encapsulate it within your own Query<> subclass to avoid the repetition throughout your app.
        // I wrote it this way for ease of testing only
        public AllCustomers(SqlDb<int> db) : base("SELECT * FROM `customer` {where}", db)
        {
        }
        
        [Where("`LastName` LIKE CONCAT('%', @lastName, '%')")]
        public string LastName { get; set; }

        [Where("`Email` LIKE CONCAT('%', @email, '%')")]
        public string Email { get; set; }

        [Where("`Phone` LIKE CONCAT('%', @phone, '%')")]
        public string Phone { get; set; }
    }
}