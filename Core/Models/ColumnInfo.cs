using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
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

		public static ColumnInfo FromPropertyInfo(PropertyInfo propertyInfo, SqlSyntax syntax)
		{
			var tbl = syntax.GetTableInfoFromType(propertyInfo.ReflectedType);
			ColumnInfo result = new ColumnInfo()
			{
				Schema = tbl.Schema,
				TableName = tbl.Name,
				ColumnName = propertyInfo.SqlColumnName(),
				PropertyInfo = propertyInfo,
				DataType = syntax.SqlDataType(propertyInfo),
				IsNullable = propertyInfo.AllowSqlNull(),
				IsCalculated = propertyInfo.HasAttribute<CalculatedAttribute>(),
				ModelType = propertyInfo.ReflectedType
			};

			DecimalPrecisionAttribute precisionAttr;
			if (propertyInfo.HasAttribute(out precisionAttr))
			{
				result.Precision = precisionAttr.Precision;
				result.Scale = precisionAttr.Scale;
			}

			CollateAttribute collateAttr;
			if (propertyInfo.HasAttribute(out collateAttr))
			{
				result.Collation = collateAttr.Collation;
			}

			return result;
		}

		public ForeignKeyInfo ToForeignKeyInfo()
		{
			return new ForeignKeyInfo()
			{
				ConstraintName = this.ForeignKeyConstraint,
				Parent = new ColumnInfo() { Schema = this.ReferencedSchema, TableName = this.ReferencedTable, ColumnName = this.ReferencedColumn },
				Child = new ColumnInfo() { Schema = this.Schema, TableName = this.TableName, ColumnName = this.ColumnName }
			};
		}

		public TableInfo GetTableInfo()
		{
			return new TableInfo(this.TableName, this.Schema, this.ModelType) { ObjectId = this.ObjectId };
		}

		public string Schema { get; set; }
		public string TableName { get; set; }
		public string ColumnName { get; set; }
		public int ObjectId { get; set; }
		public Type ModelType { get; set; }

		public string DataType { get; set; }
		public string Collation { get; set; }
		public int ByteLength { get; set; }
		public int Precision { get; set; }
		public int Scale { get; set; }
		public bool IsNullable { get; set; }
		public bool IsCalculated { get; set; }
		public string ReferencedSchema { get; set; }
		public string ReferencedTable { get; set; }
		public string ReferencedColumn { get; set; }
		public string ForeignKeyConstraint { get; set; }

		public bool IsForeignKey
		{
			get { return !string.IsNullOrEmpty(ForeignKeyConstraint); }
		}

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

		public virtual string GetSyntax()
		{
			return null;
		}

		public bool IsAlteredFrom(ColumnInfo columnInfo)
		{
			// if schema + table + name are the same....
			if (this.Equals(columnInfo))
			{
				// alter the columnInfo.DataType to reflect the size so it's comparable to how data type is reported by PropertyInfo
				if (columnInfo.DataType.Contains("var") && !columnInfo.DataType.Contains("("))
				{
					int divisor = (columnInfo.DataType.Contains("nvar")) ? 2 : 1;
					columnInfo.DataType += $"({columnInfo.ByteLength / divisor})";
				}

				// apply the scale and precision to the data type just like with length
				if (columnInfo.DataType.Contains("decimal") && !columnInfo.DataType.Contains("("))
				{
					columnInfo.DataType += $"({columnInfo.Precision}, {columnInfo.Scale})";
				}

				if (columnInfo.Length.Equals("max") && columnInfo.DataType.Equals("nvarchar(0)"))
				{
					columnInfo.DataType = "nvarchar(max)";
				}

				// then any other property diff is considered an alter
				if (!DataType?.Equals(columnInfo.DataType) ?? true) return true;
				if (IsNullable != columnInfo.IsNullable) return true;
				if (IsCalculated != columnInfo.IsCalculated) return true;

				DecimalPrecisionAttribute scaleAttr = null;
				if (PropertyInfo?.HasAttribute(out scaleAttr) ?? false)
				{
					if (Precision != columnInfo.Precision) return true;
					if (Scale != columnInfo.Scale) return true;
				}

				CollateAttribute collation = null;
				if (PropertyInfo?.HasAttribute(out collation) ?? false)
				{
					if (!collation.Collation?.Equals(columnInfo.Collation) ?? true) return true;
				}

				// note -- don't compare the ByteLength property because it's not reported by PropertyInfo
			}
			return false;
		}

		public bool InPrimaryKey
		{
			get { return PropertyInfo.HasAttribute<PrimaryKeyAttribute>(); }
		}
	}
}