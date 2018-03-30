using Postulate.Orm;
using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Testing.Models;

namespace Testing.Queries.SqlServer
{
    public class AllOrgsWithDb : Query<Organization>
    {
        public AllOrgsWithDb(SqlDb<int> db) : base("SELECT * FROM [dbo].[Organization]", db)
        {
        }
    }

	public class AllOrgs : Query<Organization>
	{
		public AllOrgs() : base("SELECT * FROM [dbo].[Organization]")
		{
		}		
	}

	public class AllOrgsOneParam : Query<Organization>
	{
		public AllOrgsOneParam() : base("SELECT * FROM [dbo].[Organization] {where}")
		{
		}

		[Where("[Name] LIKE '%'+@name+'@'")]
		public string Name { get; set; }
	}
}