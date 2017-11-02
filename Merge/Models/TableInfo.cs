using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge.Models
{
    public class TableInfo
    {
        private readonly string _schema;
        private readonly string _name;

        private const string _tempSuffix = "_temp";

        public TableInfo(string schema, string name)
        {
            _schema = schema;
            _name = name;
        }

        public string Schema { get { return _schema; } }
        public string TableName { get { return _name; } }
    }
}
