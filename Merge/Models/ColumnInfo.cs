using Postulate.Orm.Extensions;
using System;
using System.Reflection;

namespace Postulate.Orm.Merge.Models
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

        public string DataType { get; set; }
        public string Collation { get; set; }
        public int ByteLength { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsNullable { get; set; }
        public bool IsCalculated { get; set; }

        public string Length
        {
            get
            {
                if (ByteLength < 0) return "max";
                int result = ByteLength;
                if (DataType.ToLower().StartsWith("nvar")) result = result / 2;
                return $"{result}";
            }
        }

        internal bool SignatureChanged(PropertyInfo propertyInfo)
        {
            return false;
            //throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return Schema.GetHashCode() + TableName.GetHashCode() + ColumnName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ColumnInfo test = obj as ColumnInfo;
            if (test != null)
            {
                return
                    test.Schema.ToLower().Equals(this.Schema.ToLower()) &&
                    test.TableName.ToLower().Equals(this.TableName.ToLower()) &&
                    test.ColumnName.ToLower().Equals(this.ColumnName.ToLower());
            }

            PropertyInfo pi = obj as PropertyInfo;
            if (pi != null)
            {
                DbObject dbo = DbObject.FromType(pi.ReflectedType);
                return
                    dbo.Schema.ToLower().Equals(this.Schema.ToLower()) &&
                    dbo.Name.ToLower().Equals(this.TableName.ToLower()) &&
                    pi.SqlColumnName().ToLower().Equals(this.ColumnName.ToLower());
            }

            return false;
        }

        public string GetDataTypeSyntax(bool withCollation = true)
        {
            string result = null;
            switch (DataType)
            {
                case "nvarchar":
                case "char":
                case "varchar":
                case "binary":
                case "varbinary":
                    result = $"{DataType}({Length})";
                    break;

                case "decimal":
                    result = $"{DataType}({Precision}, {Scale})";
                    break;

                default:
                    result = DataType;
                    break;
            }

            if (withCollation && !string.IsNullOrEmpty(Collation))
            {
                result += " COLLATE " + Collation;
            }

            result += (IsNullable) ? " NULL" : " NOT NULL";

            return result;
        }
    }
}