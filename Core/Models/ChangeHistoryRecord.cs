using System;

namespace Postulate.Orm.Models
{
	public class ChangeHistoryRecord<TKey>
	{
		public TKey RecordId { get; set; }
		public int Version { get; set; }
		public string ColumnName { get; set; }
		public string UserName { get; set; }
		public DateTime DateTime { get; set; }
		public string OldValue { get; set; }
		public string NewValue { get; set; }
	}
}