namespace Postulate.Orm.Merge.Models
{
    public class ForeignKeyInfo
    {
        public ColumnInfo Parent { get; set; }
        public ColumnInfo Child { get; set; }
        public string ConstraintName { get; set; }
    }

    public class ForeignKeyData
    {
        public string ConstraintName { get; set; }
        public string ReferencedSchema { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
        public string ReferencingSchema { get; set; }
        public string ReferencingTable { get; set; }
        public string ReferencingColumn { get; set; }
    }
}