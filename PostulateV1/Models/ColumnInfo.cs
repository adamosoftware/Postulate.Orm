using Postulate.Orm.Extensions;
using System;
using System.Reflection;

namespace Postulate.Orm.Models
{
    public class ColumnInfo
    {
        public ColumnInfo()
        {
        }

        public static ColumnInfo FromPropertyInfo(PropertyInfo propertyInfo)
        {
            var tbl = TableInfo.FromModelType(propertyInfo.ReflectedType);
            ColumnInfo result = new ColumnInfo()
            {
                Schema = tbl.Schema,
                TableName = tbl.Name,
                ColumnName = propertyInfo.SqlColumnName()
            };

            return result;
        }

        public string Schema { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int ObjectId { get; set; }

        public string DataType { get; set; }
        public string Collation { get; set; }
        public int ByteLength { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsNullable { get; set; }
        public bool IsCalculated { get; set; }

        public PropertyInfo PropertyInfo { get; private set; }

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
                TableInfo dbo = TableInfo.FromModelType(pi.ReflectedType);
                return
                    dbo.Schema.ToLower().Equals(this.Schema.ToLower()) &&
                    dbo.Name.ToLower().Equals(this.TableName.ToLower()) &&
                    pi.SqlColumnName().ToLower().Equals(this.ColumnName.ToLower());
            }

            return false;
        }

        public override string ToString()
        {
            return $"{Schema}.{TableName}.{ColumnName}";
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

        public virtual string GetSyntax()
        {
            return null;
        }

        public bool IsAlteredFrom(ColumnInfo columnInfo)
        {
            // if schema + table + name are the same....
            if (this.Equals(columnInfo))
            {
                // then any other property diff is considered an alter
                if (!DataType?.Equals(columnInfo.DataType) ?? true) return true;
                if (ByteLength != columnInfo.ByteLength) return true;
                if (IsNullable != columnInfo.IsNullable) return true;
                if (IsCalculated != columnInfo.IsCalculated) return true;
                if (Precision != columnInfo.Precision) return true;
                if (Scale != columnInfo.Scale) return true;
                if (!Collation?.Equals(columnInfo.Collation) ?? true) return true;
            }
            return false;
        }
    }
}