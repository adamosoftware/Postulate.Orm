using Postulate.Orm.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Testing.Models
{
    [TrackChanges(IgnoreProperties = "DateModified")]
    public class TableA : BaseTable
    {
        [PrimaryKey]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [PrimaryKey]
        [MaxLength(50)]
        public string LastName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }
}
