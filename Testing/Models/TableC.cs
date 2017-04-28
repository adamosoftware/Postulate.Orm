using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Models
{
    public class TableC : BaseTable
    {
        public int SomeValue { get; set; }
        public DateTime? SomeDate { get; set; }
        public Double SomeDouble { get; set; }
    }
}
