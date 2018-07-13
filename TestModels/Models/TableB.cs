using Postulate.Orm.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Testing.Models
{
    [TrackChanges(IgnoreProperties = "DateCreated,CreatedBy")]
    [TrackDeletions]
    public class TableB : BaseTable
    {
        [ForeignKey(typeof(Organization), createIndex:true)]
        public int OrganizationId { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime? EffectiveDate { get; set; }

		[ColumnAccess(Access.InsertOnly)]
		public int? InsertOnly { get; set; }
    }
}
