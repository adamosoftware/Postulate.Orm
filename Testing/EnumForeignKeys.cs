using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.Attributes;
using Postulate.Orm.ModelMerge;
using Postulate.Orm.SqlServer;
using System.Data.SqlClient;
using System.Configuration;
using Postulate.Orm.Abstract;
using Dapper;

namespace Testing
{
    [EnumTable("SampleLookupTable", "enum")]
    public enum SampleEnumFK
    {
        Hello,
        Goodbye,
        Whatever
    }

    [TestClass]
    public class EnumForeignKeys
    {
        public class SampleModel : Record<int>
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public SampleEnumFK Greeting { get; set; }
        }

        [TestMethod]
        public void TestEnumForeignKey()
        {
            var engine = new Engine<SqlServerSyntax>(new Type[]
            {
                typeof(SampleModel)
            });

            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["PostulateWebDemo"].ConnectionString))
            {
                try
                {                    
                    cn.Execute("DROP TABLE [dbo].[SampleModel]");
                    cn.Execute("DROP TABLE [enum].[SampleLookupTable]");
                    cn.Execute("DROP SCHEMA [enum]");
                }
                catch { }

                var actions = engine.CompareAsync(cn).Result;
                string script = engine.GetScript(cn, actions).ToString();

                Assert.IsTrue(script.Equals(
					@"--  enum
CREATE SCHEMA [enum]

GO

--  Enum table SampleEnumFK
CREATE TABLE [enum].[SampleLookupTable] (
	[Name] nvarchar(50) NOT NULL,
	[Value] int NOT NULL PRIMARY KEY
)

GO

INSERT INTO [enum].[SampleLookupTable] ([Name], [Value]) VALUES ('Hello', 0)

GO

INSERT INTO [enum].[SampleLookupTable] ([Name], [Value]) VALUES ('Goodbye', 1)

GO

INSERT INTO [enum].[SampleLookupTable] ([Name], [Value]) VALUES ('Whatever', 2)

GO

--  dbo.SampleModel
CREATE TABLE [dbo].[SampleModel] (
	[Id] int identity(1,1),
	[FirstName] nvarchar(max) NULL,
	[LastName] nvarchar(max) NULL,
	[Greeting] int NOT NULL,
	CONSTRAINT [PK_SampleModel] PRIMARY KEY CLUSTERED ([Id])
)

GO

--  SampleModel.Greeting
ALTER TABLE [dbo].[SampleModel] ADD CONSTRAINT [FK_SampleModel_Greeting] FOREIGN KEY (
	[Greeting]
) REFERENCES [enum].[SampleLookupTable] (
	[Value]
)

GO

--  dbo.CustomerType
ALTER TABLE [dbo].[Customer] DROP CONSTRAINT [FK_Customer_TypeId]

GO

DROP TABLE [dbo].[CustomerType]

GO

--  dbo.Organization
ALTER TABLE [dbo].[CustomerType] DROP CONSTRAINT [FK_CustomerType_OrganizationId]

GO

ALTER TABLE [dbo].[UserProfile] DROP CONSTRAINT [FK_UserProfile_OrganizationId]

GO

ALTER TABLE [dbo].[Customer] DROP CONSTRAINT [FK_Customer_OrganizationId]

GO

DROP TABLE [dbo].[Organization]

GO

--  dbo.Region
ALTER TABLE [dbo].[Customer] DROP CONSTRAINT [FK_Customer_RegionId]

GO

ALTER TABLE [dbo].[Customer] DROP CONSTRAINT [FK_Customer_OtherRegionId]

GO

DROP TABLE [dbo].[Region]

GO

--  dbo.UserProfile
DROP TABLE [dbo].[UserProfile]

GO

--  log.QueryTrace
DROP TABLE [log].[QueryTrace]

GO

--  dbo.Customer
DROP TABLE [dbo].[Customer]

GO

"));
            }
                
        }
    }
}
