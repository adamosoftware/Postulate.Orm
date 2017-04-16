using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Postulate
{
    public class SqlServerDb : SqlDb
    {
        protected override string DelimitName(string name)
        {
            return string.Join(".", name.Split('.').Select(s => $"[{s}]"));
        }
    }
}
