using Postulate.Orm.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing.Models;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var sd = new TableASeedData();
            sd.Generate(new SqlServerDb<int>("SchemaMergeTest", "system"));
        }
    }
}
