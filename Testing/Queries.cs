using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing.Models;

namespace Testing
{
    [TestClass]
    public class Queries
    {
        [TestMethod]
        public void AnyOrgs()
        {
            var allOrgs = new AllOrgs().Execute();
            Assert.IsTrue(allOrgs.Any());
        }

        [TestMethod]
        public void OrgsWithName()
        {
            var orgs = new OrgsWithName("sample").Execute();
            Assert.IsTrue(orgs.Any());
        }
    }

    public class PostulateQuery<TResult> : Query<TResult>
    {
        public PostulateQuery(string sql) : base(sql, () => { return new PostulateDb().GetConnection(); })
        {
        }
    }

    public class AllOrgs : PostulateQuery<Organization>
    {
        public AllOrgs() : base("SELECT * FROM [dbo].[Organization]")
        {
        }
    }

    public class OrgsWithName : PostulateQuery<Organization>
    {
        public OrgsWithName(string name) : base("SELECT * FROM [dbo].[Organization] WHERE [Name] LIKE '%' + @name + '%'")
        {
            Name = name;
        }

        public string Name { get; set; }        
    }
}
