using System;

namespace Postulate.Orm.Attributes
{
    /// <summary>
    /// Type of key to create on an enum table
    /// </summary>
    public enum EnumTableKeyType
    {
        /// <summary>
        /// Let the enum table generate values (i.e. int identity(1,1) in SQL Server)
        /// </summary>
        GeneratedValues,
        /// <summary>
        /// Use the enum's explicitly defined values
        /// </summary>
        DefinedValues
    }

    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public class EnumTableAttribute : Attribute
    {
        private readonly string _schema;
        private readonly string _tableName;
        private readonly EnumTableKeyType _keyType;

        public EnumTableAttribute(string tableName, EnumTableKeyType keyType, string schema = null)
        {
            _schema = schema;
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _keyType = keyType;
        }

        public string Schema { get { return _schema; } }

        public string TableName { get { return _tableName; } }

        public EnumTableKeyType KeyType {  get { return _keyType; } }

        public string FullTableName()
        {
            return (!string.IsNullOrEmpty(Schema)) ? $"{Schema}.{TableName}" : TableName;
        }
    }
}