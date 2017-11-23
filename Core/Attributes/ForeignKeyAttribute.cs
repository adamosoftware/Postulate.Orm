using System;

namespace Postulate.Orm.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        private readonly string _columnName;
        private readonly Type _primaryTable;
        private readonly bool _cascadeDelete;
        private readonly bool _createIndex;

        /// <summary>
        /// At the class level, denotes a foreign key applied to a column in any table with a given name
        /// </summary>
        public ForeignKeyAttribute(string columnName, Type primaryTable, bool cascadeDelete = false, bool createIndex = false)
        {
            _columnName = columnName;
            _primaryTable = primaryTable;
            _cascadeDelete = false;
        }

        /// <summary>
        /// On a single property, denotes a foreign key
        /// </summary>
        public ForeignKeyAttribute(Type primaryTable, bool cascadeDelete = false, bool createIndex = false)
        {
            _primaryTable = primaryTable;
            _cascadeDelete = cascadeDelete;
            _createIndex = createIndex;
        }

        public string ColumnName { get { return _columnName; } }

        public Type PrimaryTableType { get { return _primaryTable; } }

        public bool CascadeDelete { get { return _cascadeDelete; } }

        public bool CreateIndex { get { return _createIndex; } }
    }
}