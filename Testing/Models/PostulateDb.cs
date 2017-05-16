using Postulate.Orm;
using System.Configuration;

namespace Testing.Models
{
    public class PostulateDb : SqlServerDb<int>
    {
        private const string dbName = "PostulateTest";

        public PostulateDb(Configuration configuration) : base(configuration, dbName)
        {
        }

        public PostulateDb() : base(dbName, "adamo")
        {
        }
    }
}
