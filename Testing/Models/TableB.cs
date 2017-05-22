using Postulate.Orm.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Models
{
    class TableB : BaseTable
    {
        [ForeignKey(typeof(Organization), createIndex:true)]
        public int OrganizationId { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }
    }
}
