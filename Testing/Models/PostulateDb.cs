using Postulate.Orm.SqlServer;
using System.Configuration;

namespace Testing.Models
{
    public class PostulateDb : SqlServerDb<int>
    {
        private const string dbName = "PostulateTest";

        public PostulateDb(Configuration configuration) : base(configuration, dbName)
        {
            MergeExcludeSchemas = "HangFire,Whatsit,Nevermore";
            MergeExcludeTables = "Rhomboid,Occlusus";
        }

        public PostulateDb() : base(dbName, "adamo")
        {
            MergeExcludeSchemas = "HangFire,Whatsit,Nevermore";
            MergeExcludeTables = "Rhomboid,Occlusus";
        }
    }
}