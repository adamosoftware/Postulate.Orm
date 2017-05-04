using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate;
using Postulate.Abstract;
using Postulate.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
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

        [TestMethod]
        public void OrgsWithName2()
        {
            var orgs = new OrgsWithName2() { Name = "sample" }.Execute();
            Assert.IsTrue(orgs.Any());
        }

        [TestMethod]
        public void OrgsPaged()
        {
            int totalRecordCount = new PostulateQuery<int>("SELECT COUNT(1) FROM [dbo].[Organization]").ExecuteSingle();
            int testRecords = 0;

            IEnumerable<Organization> orgs = null;
            int page = 0;
            do
            {
                orgs = new AllOrgs().Execute("[Name]", 3, page);
                testRecords += orgs.Count();
                page++;
            } while (orgs.Any());

            Assert.IsTrue(testRecords == totalRecordCount);
        }

        [TestMethod]
        public void OrgsWhere()
        {
            Organization org = new Organization() { Name = "Sample Org For You", BillingRate = 1 };

            var db = new PostulateDb();
            db.Save(org);

            var orgs = new OrgsWhere() { Name = "sample" }.Execute();
            Assert.IsTrue(orgs.Any());

            db.Delete<Organization>(org.Id);
        }

        [TestMethod]
        public void QueryTestMethod()
        {
            PostulateQuery<Organization>[] queries = new PostulateQuery<Organization>[]
            {
                new AllOrgs(), new OrgsWithName2(), new OrgsWhere()
            };

            using (IDbConnection cn = new PostulateDb().GetConnection())
            {
                cn.Open();
                foreach (var query in queries) query.Test(cn);
            }                
        }
    }

    public class PostulateQuery<TResult> : Query<TResult>
    {
        public PostulateQuery(string sql) : base(sql, () => { return new PostulateDb().GetConnection(); })
        {
        }

        //public override string[] SortOptions { get { return new string[] { "[Name]" }; }

        public override SortOption[] SortOptions => new SortOption[]
            {
                new SortOption() { Text = "Last Name", Expression = "[LastName] ASC" }
            };
    }

    public class AllOrgs : PostulateQuery<Organization>
    {
        public AllOrgs() : base("SELECT * FROM [dbo].[Organization] {where}")
        {
        }

        [Where("[Name] LIKE '%' + @name + '%'")]
        public string Name { get; set; }
    }

    public class OrgsWithName : PostulateQuery<Organization>
    {
        public OrgsWithName(string name) : base("SELECT * FROM [dbo].[Organization] WHERE [Name] LIKE '%' + @name + '%'")
        {
            Name = name;
        }

        public string Name { get; set; }        
    }

    public class OrgsWithName2 : PostulateQuery<Organization>
    {
        public OrgsWithName2() : base("SELECT * FROM [dbo].[Organization] WHERE [Name] LIKE '%' + @name + '%'")
        {
        }

        public string Name { get; set; }
    }

    public class OrgsWhere : PostulateQuery<Organization>
    {
        public OrgsWhere() : base("SELECT * FROM [dbo].[Organization] {where}")
        {
        }

        [Where("[Name] LIKE '%' + @name + '%'")]
        public string Name { get; set; }
    }
}
