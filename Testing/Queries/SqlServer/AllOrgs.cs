using Postulate.Orm;
using Postulate.Orm.Abstract;
using Testing.Models;

namespace Testing.Queries.SqlServer
{
    public class AllOrgs : Query<Organization>
    {
        public AllOrgs(SqlDb<int> db) : base("SELECT * FROM [dbo].[Organization]", db)
        {
        }
    }
}