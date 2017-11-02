using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Orm;
using Postulate.Orm.Merge;
using Sample.Models;
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
                sm.Validate(cn, results);
                //var script = sm.GetScript(cn, results);
                //sm.Execute(cn, results);
                sm.SaveScriptAs(@"C:\Users\Adam\Desktop\Merge.sql");
            }            
        }

        [TestMethod]
        public void TestSampleWebApp()
        {
            SchemaMerge<Sample.Models.DemoDb> sm = new SchemaMerge<Sample.Models.DemoDb>();
            using (IDbConnection cn = sm.GetConnection())
            {
                cn.Open();
                sm.SaveScriptAs(@"C:\Users\Adam\Desktop\Merge.sql");
            }
        }
    }
}

