using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using Testing.Models;
using System.Data;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge;
using Dapper;
using System.Linq;

namespace Testing
{
    [TestClass]
    public class SchemaMergeTest
    {
        [TestInitialize]
        public void DropTestTables()
        {
            var db = new PostulateDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();
                DropTablesIfExists(cn, "TableA", "TableB", "TableC");
            }
        }

        private void DropTablesIfExists(IDbConnection cn, params string[] tableNames)
        {
            // note the tables must be in safe FK drop order

            foreach (var tbl in tableNames)
            {
                DbObject obj = DbObject.Parse(tbl);
                if (cn.TableExists(obj.Schema, obj.Name))
                {
                    cn.Execute($"DROP TABLE [{obj.Schema}].[{obj.Name}]");
                }
            }
        }

        private bool AllTablesExist(IDbConnection cn, params string[] tableNames)
        {
            return tableNames.All(item =>
            {
                DbObject obj = DbObject.Parse(item, cn);
                return cn.TableExists(obj.Schema, obj.Name);
            });
        }

        private bool AllColumnsExist(IDbConnection cn, string tableName, params string[] columnNames)
        {
            return columnNames.All(item =>
            {
                DbObject obj = DbObject.Parse(tableName);
                return cn.ColumnExists(obj.Schema, obj.Name, item);
            });
        }

        private bool PKColumnsEqual(IDbConnection cn, string tableName, params string[] columnNames)
        {
            DbObject obj = DbObject.Parse(tableName, cn);
            var pkColumns = cn.Query<string>(
                @"SELECT 
                    LOWER([col].[name]) AS [ColumnName]
                FROM 
	                [sys].[index_columns] [ixcol] INNER JOIN [sys].[columns] [col] ON
		                [ixcol].[object_id]=[col].[object_id] AND
		                [ixcol].[column_id]=[col].[column_id]
	                INNER JOIN [sys].[indexes] [ix] ON
		                [ixcol].[object_id]=[ix].[object_id] AND
		                [ixcol].[index_id]=[ix].[index_id]
                WHERE 
	                [ix].[is_primary_key]=1 AND
	                [col].[object_id]=@objectId
                ORDER BY [col].[name]", new { objectId = obj.ObjectId });

            return Enumerable.SequenceEqual(columnNames.Select(s => s.ToLower()).OrderBy(s => s), pkColumns);
        }

        [TestMethod]
        public void DetectNewTables()
        {
            var sm = new SchemaMerge<PostulateDb>();
            var diffs = sm.Compare();

            Assert.IsTrue(
                diffs.Any(item1 =>
                    item1.ActionType == MergeActionType.Create &&
                    item1.ObjectType == MergeObjectType.Table &&
                    item1.Description.Contains("TableA") &&
                diffs.Any(item2 =>
                    item2.ActionType == MergeActionType.Create &&
                    item2.ObjectType == MergeObjectType.Table &&
                    item2.Description.Contains("TableB"))));
        }

        [TestMethod]
        public void CreateNewTables()
        {
            var sm = new SchemaMerge<PostulateDb>();

            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();
                sm.Execute(cn);
                Assert.IsTrue(AllTablesExist(cn, "TableA", "TableB", "TableC"));
            }
        }

        [TestMethod]
        public void CreateNewNonKeyColumns()
        {
            var sm = new SchemaMerge<PostulateDb>();

            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();

                // create tables A, B, and C
                sm.Execute(cn);

                // drop a few columns
                cn.Execute("ALTER TABLE [dbo].[TableC] DROP COLUMN [SomeDate]");
                cn.Execute("ALTER TABLE [dbo].[TableC] DROP COLUMN [SomeDouble]");

                // add them back
                sm.Execute(cn);

                Assert.IsTrue(AllColumnsExist(cn, "TableC", "SomeDate", "SomeDouble"));
            }
        }

        [TestMethod]
        public void CreateNewKeyColumnsInEmptyTable()
        {
            var sm = new SchemaMerge<PostulateDb>();

            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();

                // create tables A, B, and C
                sm.Execute(cn);

                // drop PK on table A
                cn.Execute("ALTER TABLE [dbo].[TableA] DROP CONSTRAINT [PK_TableA]");
                cn.Execute("ALTER TABLE [dbo].[TableA] DROP COLUMN [LastName]");

                // restore them along with primary key
                sm.Execute(cn);

                Assert.IsTrue(PKColumnsEqual(cn, "TableA", "FirstName", "LastName"));
            }
        }

        [TestMethod]
        public void CreateNewKeyColumnsInNonEmptyTable()
        {
            var sm = new SchemaMerge<PostulateDb>();            

            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();

                // create tables A, B, and C
                sm.Execute(cn);

                var record = new TableA() { FirstName = "Adam", LastName = "O'Neil" };
                new PostulateDb().Save(record);


            }
        }

        [TestMethod]
        public void CreateIfNotExists()
        {
            var sm = new SchemaMerge<Models.Tdg.CreateIfNotExistsDb>();
            sm.CreateIfNotExists();
        }
    }
}

