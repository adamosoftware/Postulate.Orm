using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;

namespace TestModels.Models
{
	[NoIdentity]
	public class TableD : Record<int>
	{
		public string FieldOne { get; set; }
	}
}