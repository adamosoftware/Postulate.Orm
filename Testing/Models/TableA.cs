using Postulate.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Models
{
    public class TableA : BaseTable
    {
        [PrimaryKey]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [PrimaryKey]
        [MaxLength(50)]
        public string LastName { get; set; }
    }
}
