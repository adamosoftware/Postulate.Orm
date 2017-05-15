using System;
using System.ComponentModel.DataAnnotations;
using Postulate.Orm.Attributes;

namespace Testing.Models
{
    public class Organization : BaseTable
	{
		[MaxLength(100)]
		[Required]
		[PrimaryKey]		
		public string Name { get; set; }		

		[MaxLength(255)]
		public string Description { get; set; }

		public DateTime? EffectiveDate { get; set; }
		public decimal BillingRate { get; set; }

		public DateTime? EndDate { get; set; }

        //public int? SomeNewValue { get; set; }

		[Calculated("DATEDIFF(d, [EffectiveDate], [EndDate])")]
		public int? ContractLength { get; set; }
	}
}
