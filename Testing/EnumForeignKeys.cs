using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge;
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

            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
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
                    @"--  Enum table SampleEnumFK
CREATE TABLE [enum].[SampleLookupTable] (
	[Name] nvarchar(50) NOT NULL,
	[Value] int identity (1,1) NOT NULL PRIMARY KEY
)

GO

INSERT INTO [enum].[SampleLookupTable] ([Name]) VALUES ('Hello')

GO

INSERT INTO [enum].[SampleLookupTable] ([Name]) VALUES ('Goodbye')

GO

INSERT INTO [enum].[SampleLookupTable] ([Name]) VALUES ('Whatever')

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

"));
            }
                
        }
    }
}
