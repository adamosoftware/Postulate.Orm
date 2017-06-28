using Postulate.Orm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Models.Tdg
{
    public class CreateIfNotExistsDb : SqlServerDb<int>
    {
        public CreateIfNotExistsDb() : base("CreateIfNotExistsDb")
        {
        }
    }
}
