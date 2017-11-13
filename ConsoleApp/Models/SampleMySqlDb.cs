using Postulate.Orm.MySql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp.Models
{
    public class SampleMySqlDb : MySqlDb<int>
    {
        public SampleMySqlDb() : base("HelloMySql", "system")
        {
        }
    }
}
