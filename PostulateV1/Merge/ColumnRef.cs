using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge
{
    public class ColumnRef
    {
        public ColumnRef(PropertyInfo pi)
        {
            PropertyInfo = pi;
            DbObject obj = DbObject.FromType(pi.ReflectedType);
            Schema = obj.Schema;
            TableName = obj.Name;
            ColumnName = pi.SqlColumnName();
            DataType = pi.SqlDataType();
            CollateAttribute collate;
            if (pi.HasAttribute(out collate)) Collation = collate.Collation;
            DbObject = obj;
            IsNullable = pi.AllowSqlNull();
            ModelType = pi.ReflectedType;
        }

        internal static string CompareSyntaxes(ColumnRef leftColumn, ColumnRef rightColumn)
        {
            bool withCollation = IsCollationChanged(leftColumn, rightColumn);
            return $"{leftColumn.GetDataTypeSyntax(withCollation)} -> {rightColumn.GetDataTypeSyntax(withCollation)}";
        }

        public ColumnRef()
        {
        }

        public string Schema { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public int ObjectID { get; set; }
        public DbObject DbObject { get; set; }
        public Type ModelType { get; set; }

        public string DataType { get; set; }
        public string Collation { get; set; }
        public int ByteLength { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }

        internal Type FindModelType(IEnumerable<Type> modelTypes)
        {
            return modelTypes.FirstOrDefault(t =>
            {
                DbObject obj = DbObject.FromType(t);
                return obj.Schema.Equals(Schema) && obj.Name.Equals(TableName);
            });
        }

        public bool IsNullable { get; set; }

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

        internal static bool IsCollationChanged(ColumnRef leftColumn, ColumnRef rightColumn)
        {
            if (string.IsNullOrEmpty(leftColumn.Collation) && string.IsNullOrEmpty(rightColumn.Collation)) return false;
            return !leftColumn?.Collation?.Equals(rightColumn?.Collation) ?? true;
        }

        public override bool Equals(object obj)
        {
            ColumnRef test = obj as ColumnRef;
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

        public override int GetHashCode()
        {
            return Schema.GetHashCode() + TableName.GetHashCode() + ColumnName.GetHashCode();
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

        public string DataTypeComparison(ColumnRef columnRef)
        {
            return $"{this.GetDataTypeSyntax()} -> {columnRef.PropertyInfo.SqlColumnType()}";
        }

        internal bool IsForeignKey(IDbConnection connection, out ForeignKeyRef fk)
        {
            fk = null;
            var result = connection.QueryFirstOrDefault(
                @"SELECT
					[fk].[name] AS [ConstraintName], [t].[name] AS [TableName], SCHEMA_NAME([t].[schema_id]) AS [Schema]
				FROM
					[sys].[foreign_key_columns] [fkcol] INNER JOIN [sys].[columns] [col] ON
						[fkcol].[parent_object_id]=[col].[object_id] AND
						[fkcol].[parent_column_id]=[col].[column_id]
					INNER JOIN [sys].[foreign_keys] [fk] ON [fkcol].[constraint_object_id]=[fk].[object_id]
					INNER JOIN [sys].[tables] [t] ON [fkcol].[parent_object_id]=[t].[object_id]
				WHERE
					SCHEMA_NAME([t].[schema_id])=@schema AND
					[t].[name]=@tableName AND
					[col].[name]=@columnName", new { schema = this.Schema, tableName = this.TableName, columnName = this.ColumnName });

            if (result != null)
            {
                fk = new ForeignKeyRef()
                {
                    ConstraintName = result.ConstraintName,
                    ChildObject = new DbObject(result.Schema, result.TableName)
                };
                return true;
            }

            return false;
        }

        internal bool InPrimaryKey(IDbConnection connection, out string constraintName, out bool isClustered)
        {
            constraintName = null;
            isClustered = false;

            var result = connection.QueryFirstOrDefault(
                @"SELECT
					[ndx].[name] AS [ConstraintName], CONVERT(bit, CASE [ndx].[type_desc] WHEN 'CLUSTERED' THEN 1 ELSE 0 END) AS [IsClustered]
				FROM
					[sys].[indexes] [ndx] INNER JOIN [sys].[index_columns] [ndxcol] ON [ndx].[object_id]=[ndxcol].[object_id]
					INNER JOIN [sys].[columns] [col] ON
						[ndxcol].[column_id]=[col].[column_id] AND
						[ndxcol].[object_id]=[col].[object_id]
					INNER JOIN [sys].[tables] [t] ON [col].[object_id]=[t].[object_id]
				WHERE
					[is_primary_key]=1 AND
					SCHEMA_NAME([t].[schema_id])=@schema AND
					[t].[name]=@tableName AND
					[col].[name]=@columnName", new { schema = this.Schema, tableName = this.TableName, columnName = this.ColumnName });
            if (result != null)
            {
                constraintName = result.ConstraintName;
                isClustered = result.IsClustered;
                return true;
            }

            return false;
        }

        internal bool HasDefault(IDbConnection connection, out string constraintName)
        {
            constraintName = null;

            var result = connection.QueryFirstOrDefault(
                @"SELECT
						[df].[name] AS [ConstraintName]
					FROM
						[sys].[columns] [col] INNER JOIN [sys].[default_constraints] [df] ON [col].[default_object_id]=[df].[object_id]
						INNER JOIN [sys].[tables] [t] ON [col].[object_id]=[t].[object_id]
					WHERE
						SCHEMA_NAME([t].[schema_id])=@schema AND
						[t].[name]=@tableName AND
						[col].[name]=@columnName", new { schema = this.Schema, tableName = this.TableName, columnName = this.ColumnName });
            if (result != null)
            {
                constraintName = result.ConstraintName;
                return true;
            }

            return false;
        }
    }
}