using Postulate;
using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Models
{
    public class PostulateDb : SqlServerDb<int>
    {
        public PostulateDb() : base("PostulateTest", "adamo")
        {
        }
    }
}
