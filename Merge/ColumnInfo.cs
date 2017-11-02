using System;
using System.Reflection;

namespace Postulate.Orm.Merge
{
    public class ColumnInfo
    {
        private readonly PropertyInfo _propertyInfo;

        public ColumnInfo()
        {
        }

        public ColumnInfo(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public string Schema { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }

        internal bool SignatureChanged(PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }
    }
}