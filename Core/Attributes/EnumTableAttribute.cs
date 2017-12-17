using System;

namespace Postulate.Orm.Attributes
{
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public class EnumTableAttribute : Attribute
    {
        private readonly string _tableName;

        public EnumTableAttribute(string tableName)
        {
            _tableName = tableName;
        }
    }
}