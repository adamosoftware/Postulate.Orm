namespace Postulate.Orm.Merge.Models
{
    public class TableInfo
    {
        private readonly string _schema;
        private readonly string _name;

        public TableInfo(string schema, string name)
        {
            _schema = schema;
            _name = name;
        }

        public string Schema { get { return _schema; } }
        public string TableName { get { return _name; } }
    }
}