using Postulate;
using Postulate.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Testing.Models
{
	//[UniqueKey(nameof(OrganizationID), nameof(UpcCode))]
	[UniqueKey("U_Item_OrgUpc", new string[] { nameof(OrganizationID), nameof(UpcCode) })]
	public class Item : BaseTable
	{
		[PrimaryKey]
		public int OrganizationID { get; set; }
		[PrimaryKey]		
		[MaxLength(100)]		
		public string Name { get; set; }

		[MaxLength(50)]        
		public string UpcCode { get; set; }

		[MaxLength(255)]		
		public string Description { get; set; }
		
		[DecimalPrecision(2, 2)]
		public decimal UnitCost { get; set; }
		[DecimalPrecision(2, 2)]
		public decimal UnitPrice { get; set; }
		[DecimalPrecision(2, 2)]
		public decimal QtyOnHand { get; set; }
		public decimal ReorderQty { get; set; }		
		//[Column(TypeName = "date")]
		//public DateTime? EffectiveDate { get; set; }

	}
}
