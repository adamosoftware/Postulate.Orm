using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm;
using Postulate.Orm.Merge;
using PostulateDemo2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing
{
    [TestClass]
    public class SchemaMerge2Test
    {
        [TestMethod]
        public void TestAbstractModelAdded()
        {
            SchemaMerge<DemoDb> sm = new SchemaMerge<DemoDb>();
            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();
                var results = sm.Compare(cn);
                //var script = sm.GetScript(cn, results);
                //sm.Execute(cn, results);
                sm.SaveScriptAs(@"C:\Users\Adam\Desktop\Merge.sql");
            }                     
        }
    }
}
