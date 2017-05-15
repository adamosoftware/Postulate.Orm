using Postulate.Orm;

namespace Testing.Models
{
    public class PostulateDb : SqlServerDb<int>
    {
        public PostulateDb() : base("PostulateTest", "adamo")
        {
        }
    }
}
