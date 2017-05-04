using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using Testing.Models;
using Dapper;
using Postulate;
using System.Linq;
using System.Collections.Generic;

namespace Testing
{
    [TestClass]
    public class SqlTest
    {
        [TestMethod]
        public void DynamicWhereClausesTable()
        {
            using (IDbConnection cn = new PostulateDb().GetConnection())
            {
                cn.Open();
                var results = InnerQuery(cn, "Org", null);
                Assert.IsTrue(results.Any());
            }
        }

        [TestMethod]
        public void DynamicWhereClausesColumn()
        {
            using (IDbConnection cn = new PostulateDb().GetConnection())
            {
                cn.Open();
                var results = InnerQuery(cn, null, "last");
                Assert.IsTrue(results.Any());
            }
        }

        private static IEnumerable<TableInfo> InnerQuery(IDbConnection cn, string tableName, string columnName)
        {
            DynamicParameters p;            
            var results = cn.Query<TableInfo>(
                $@"SELECT 
					    SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [Name], [object_id] AS [ObjectId]
				    FROM 
					    [sys].[tables] [t]				    		
				    {Sql.WhereClause(new WhereClauseTerm[]
                    {
                        new WhereClauseTerm(!string.IsNullOrEmpty(tableName), "[name] LIKE '%'+@table+'%'", tableName),
                        new WhereClauseTerm(!string.IsNullOrEmpty(columnName), "EXISTS(SELECT 1 FROM [sys].[columns] WHERE [object_id]=[t].[object_id] AND [name] LIKE '%'+@column+'%')", columnName)
                    }, out p)}					
				ORDER BY 
					[name]", p);
            return results;
        }
    }

    internal class TableInfo
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public int ObjectId { get; set; }
    }

}
