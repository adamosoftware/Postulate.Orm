namespace Postulate.Orm.Models
{
	public class KeyColumnInfo
	{
		public string Schema { get; set; }
		public string TableName { get; set; }
		public string ColumnName { get; set; }
		public string IndexName { get; set; }
		public byte IndexType { get; set; }
		public bool IsUnique { get; set; }
		public bool IsPrimaryKey { get; set; }

		public override bool Equals(object obj)
		{
			ColumnInfo cr = obj as ColumnInfo;
			if (cr != null)
			{
				return
					Schema.ToLower().Equals(cr.Schema.ToLower()) &&
					TableName.ToLower().Equals(cr.TableName.ToLower()) &&
					ColumnName.ToLower().Equals(cr.ColumnName.ToLower());
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}