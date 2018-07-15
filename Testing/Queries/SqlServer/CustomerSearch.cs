using System.Data;
using Dapper;
using Postulate.Orm;
using Postulate.Orm.Interfaces;
using Testing.Models;

namespace Testing.Queries.SqlServer
{
	public class CustomerSearch : Query<Customer>
	{
		public CustomerSearch(ISqlDb db) : base(
			@"SELECT * FROM [dbo].[Customer] 
			WHERE 
				[OrganizationId]=@orgId AND
				([LastName] LIKE '%' + @search + '%' OR [FirstName] LIKE '%' + @search + '%')", db)
		{
		}

		protected override string OnQueryResolved(IDbConnection connection, string query, DynamicParameters parameters)
		{
			return base.OnQueryResolved(connection, query, parameters);
		}

		public int OrgId { get; set; }
		public string Search { get; set; }
	}
}