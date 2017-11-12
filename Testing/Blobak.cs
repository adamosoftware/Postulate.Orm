using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Testing
{
    [TestClass]
    public class Blobak
    {
        private static string ConnectionString(string database)
        {
            return $@"Data Source=(localdb)\MSSQLLOCALDB;Database={database};Integrated Security=true";
        }

        private const string dbName = "BlobakBlankDb";

        /*
        [TestMethod]
        public void SchemaMergeBlankDb()
        {
            using (var cn = new SqlConnection(ConnectionString("master")))
            {
                cn.Open();
                cn.Execute($@"
                    IF EXISTS(SELECT 1 FROM sys.databases WHERE name='{dbName}')
                    BEGIN
                        ALTER DATABASE [{dbName}] SET single_user with rollback immediate
                        DROP DATABASE [{dbName}]
                    END"); // thanks top https://stackoverflow.com/questions/1711840/how-do-i-specify-close-existing-connections-in-sql-script

                cn.Execute($"CREATE DATABASE [{dbName}]");
            }

            var sm = new SchemaMerge<BlobakLib.Models.BackupDb>();
            using (var cn = new SqlConnection(ConnectionString(dbName)))
            {
                cn.Open();
                sm.Execute(cn);
            }
        }*/

        /*
        [TestMethod]
        public void SchemaMergeRecoverFKs()
        {
            SchemaMergeBlankDb();

            string[] fkNames = new string[]
            {
                "FK_BackupJob_AccountId", "FK_Error_JobId", "FK_Error_JobId", "FK_Version_BlobId"
            };

            // delete a few FKs
            using (var cn = new SqlConnection(ConnectionString(dbName)))
            {
                cn.Open();
                cn.Execute(@"
                    ALTER TABLE [dbo].[BackupJob] DROP CONSTRAINT [FK_BackupJob_AccountId]
                    ALTER TABLE [dbo].[Error] DROP CONSTRAINT [FK_Error_JobId]
                    ALTER TABLE [dbo].[Version] DROP CONSTRAINT [FK_Version_JobId]
                    ALTER TABLE [dbo].[Version] DROP CONSTRAINT [FK_Version_BlobId]");

                var sm = new SchemaMerge<BackupDb>();
                sm.Execute(cn);

                Assert.IsTrue(fkNames.All(s => cn.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = s })));
            }
        }
        */
    }
}