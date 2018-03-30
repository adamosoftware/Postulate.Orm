using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing.Models;

namespace Testing
{
    [TestClass]
    public class CommandBuilding
    {
        [TestMethod]
        public void CheckColumnAccess()
        {
            // we're checking to see that OrganizationId is not part of the update list
            var db = new PostulateDbNested();
            TableB b = new TableB() { Id = 232, OrganizationId = 1, Description = "whatever", InsertOnly = 34343 };
            string cmd = db.GetUpdateStatement(b);
            Assert.IsTrue(cmd.Equals(@"UPDATE [dbo].[TableB] SET
                    [OrganizationId]=@OrganizationId, [Description]=@Description, [EffectiveDate]=@EffectiveDate, [DateModified]=@DateModified, [ModifiedBy]=@ModifiedBy
                WHERE
                    [Id]=@id"));
        }

    }

    internal class PostulateDbNested : PostulateDb
    {
        public string GetUpdateStatement<TRecord>(TRecord record) where TRecord : Record<int>
        {
            return GetUpdateStatement<TRecord>();
        }
    }
}
