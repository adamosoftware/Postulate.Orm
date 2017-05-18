using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using ReflectionHelper;

namespace Postulate.Orm.Merge.Action
{
    public class CreateTable : MergeAction
    {
        private readonly Type _modelType;
        private readonly string _schema;
        private readonly string _name;

        public CreateTable(Type modelType) : base(MergeObjectType.Table, MergeActionType.Create, $"Create table {TableName(modelType)}")
        {
            if (modelType.HasAttribute<NotMappedAttribute>()) throw new InvalidOperationException($"The model class {modelType.Name} is marked as [NotMapped]");

            _modelType = modelType;
            ParseNameAndSchema(modelType, out _schema, out _name);
        }

        internal bool InPrimaryKey(string columnName, out string pkName)
        {
            pkName = $"PK_{DbObject.ConstraintName(_modelType)}";
            return _modelType.GetProperties().Any(pi => pi.SqlColumnName().Equals(columnName) && pi.HasAttribute<PrimaryKeyAttribute>());
        }

        public string Schema { get { return _schema; } }
        public string Name { get { return _name; } }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            if (_modelType.GetProperties().Any(pi => pi.HasAttribute<UniqueKeyAttribute>(attr => attr.IsClustered)) && _modelType.HasAttribute<ClusterAttribute>())
            {
                yield return "Model class with [Cluster] attribute may not have properties with a clustered unique key.";
            }

            foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<PrimaryKeyAttribute>())))
            {
                if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Primary key column [{pi.Name}] may not use MAX size.";
                if (pi.PropertyType.IsNullableGeneric()) yield return $"Primary key column [{pi.Name}] may not be nullable.";
            }

            foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<UniqueKeyAttribute>())))
            {
                if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
            }

            // class-level unique with MAX
            var uniques = _modelType.GetCustomAttributes<UniqueKeyAttribute>();
            foreach (var u in uniques)
            {
                foreach (var col in u.ColumnNames)
                {
                    PropertyInfo pi = _modelType.GetProperty(col);
                    if (pi.SqlColumnType().ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
                }
            }
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            yield return
                $"CREATE TABLE {TableName(_modelType)} (\r\n\t" +
                    string.Join(",\r\n\t", CreateTableMembers()) +
                "\r\n)";
        }

        public static string TableName(Type modelType)
        {
            string schema, name;
            ParseNameAndSchema(modelType, out schema, out name);
            return $"[{schema}].[{name}]";
        }

        public static void ParseNameAndSchema(Type modelType, out string schema, out string name)
        {
            schema = "dbo";
            name = modelType.Name;
            bool hasTableAttributeSchema = false;

            TableAttribute attr;
            if (modelType.HasAttribute(out attr))
            {
                if (!string.IsNullOrEmpty(attr.Name)) name = attr.Name;
                if (!string.IsNullOrEmpty(attr.Schema))
                {
                    hasTableAttributeSchema = true;
                    schema = attr.Schema;
                }
            }

            SchemaAttribute schemaAttr;
            if (modelType.HasAttribute(out schemaAttr))
            {
                if (hasTableAttributeSchema) throw new InvalidOperationException($"Model class {modelType.Name} may not have both [Table] and [Schema] attributes.");
                schema = schemaAttr.Schema;
            }
        }

        public static Dictionary<Type, string> SupportedTypes(string length = null, byte precision = 0, byte scale = 0)
        {
            return new Dictionary<Type, string>()
            {
                { typeof(string), $"nvarchar({length})" },
                { typeof(bool), "bit" },
                { typeof(int), "int" },
                { typeof(decimal), $"decimal({precision}, {scale})" },
                { typeof(double), "float" },
                { typeof(float), "float" },
                { typeof(long), "bigint" },
                { typeof(short), "smallint" },
                { typeof(byte), "tinyint" },
                { typeof(Guid), "uniqueidentifier" },
                { typeof(DateTime), "datetime" },
                { typeof(TimeSpan), "time" },
                { typeof(char), "nchar(1)" },
                { typeof(byte[]), $"varbinary({length})" }
            };
        }

        public static IEnumerable<string> PrimaryKeyColumns(Type modelType, bool markedOnly = false)
        {
            var pkColumns = modelType.GetProperties().Where(pi => pi.HasAttribute<PrimaryKeyAttribute>()).Select(pi => pi.SqlColumnName());

            if (pkColumns.Any() || markedOnly) return pkColumns;

            return new string[] { modelType.IdentityColumnName() };
        }

        private string[] CreateTableMembers()
        {
            List<string> results = new List<string>();

            ClusterAttribute clusterAttribute = GetClusterAttribute();

            results.AddRange(CreateTableColumns());

            results.Add(CreateTablePrimaryKey(clusterAttribute));

            results.AddRange(CreateTableUniqueConstraints(clusterAttribute));

            return results.ToArray();

        }

        internal ClusterAttribute GetClusterAttribute()
        {
            return _modelType.GetCustomAttribute<ClusterAttribute>() ?? new ClusterAttribute(ClusterOption.PrimaryKey);
        }

        private IEnumerable<string> CreateTableForeignKeys()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<string> CreateTableColumns()
        {
            List<string> results = new List<string>();

            Position identityPos = Position.StartOfTable;
            var ip = _modelType.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip == null) ip = _modelType.BaseType.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip != null) identityPos = ip.Position;

            if (identityPos == Position.StartOfTable) results.Add(IdentityColumnSql());

            results.AddRange(_modelType.GetProperties()
                .Where(p =>
                    p.CanWrite &&
                    !p.Name.ToLower().Equals(nameof(Record<int>.Id).ToLower()) &&
                    !p.HasAttribute<NotMappedAttribute>())
                .Select(pi =>
                {
                    return pi.SqlColumnSyntax();
                }));

            if (identityPos == Position.EndOfTable) results.Add(IdentityColumnSql());

            return results;
        }

        private string IdentityColumnSql()
        {
            var typeMap = new Dictionary<Type, string>()
            {
                { typeof(int), "int identity(1,1)" },
                { typeof(long), "bigint identity(1,1)" },
                { typeof(Guid), "uniqueidentifier DEFAULT NewSequentialID()" }
            };

            Type keyType = FindKeyType(_modelType);

            return $"[{_modelType.IdentityColumnName()}] {typeMap[keyType]}";
        }
       
        internal string CreateTablePrimaryKey(ClusterAttribute clusterAttribute)
        {
            return $"CONSTRAINT [PK_{DbObject.ConstraintName(_modelType)}] PRIMARY KEY {clusterAttribute.Syntax(ClusterOption.PrimaryKey)}({string.Join(", ", PrimaryKeyColumns().Select(col => $"[{col}]"))})";
        }        

        private Type FindKeyType(Type modelType)
        {
            if (!modelType.IsDerivedFromGeneric(typeof(Record<>))) throw new ArgumentException("Model class must derive from Record<TKey>");

            Type checkType = modelType;
            while (!checkType.IsGenericType) checkType = checkType.BaseType;
            return checkType.GetGenericArguments()[0];
        }

        private IEnumerable<string> CreateTableUniqueConstraints(ClusterAttribute clusterAttribute)
        {
            List<string> results = new List<string>();

            if (PrimaryKeyColumns(markedOnly: true).Any())
            {
                results.Add($"CONSTRAINT [U_{DbObject.ConstraintName(_modelType)}_Id] UNIQUE {clusterAttribute.Syntax(ClusterOption.Identity)}([Id])");
            }

            results.AddRange(_modelType.GetProperties().Where(pi => pi.HasAttribute<UniqueKeyAttribute>()).Select(pi =>
            {
                UniqueKeyAttribute attr = pi.GetCustomAttribute<UniqueKeyAttribute>();
                return $"CONSTRAINT [U_{DbObject.ConstraintName(_modelType)}_{pi.SqlColumnName()}] UNIQUE {attr.GetClusteredSyntax()}([{pi.SqlColumnName()}])";
            }));

            results.AddRange(_modelType.GetCustomAttributes<UniqueKeyAttribute>().Select((u, i) =>
            {
                string constrainName = (string.IsNullOrEmpty(u.ConstraintName)) ? $"U_{DbObject.ConstraintName(_modelType)}_{i}" : u.ConstraintName;
                return $"CONSTRAINT [{constrainName}] UNIQUE {u.GetClusteredSyntax()}({string.Join(", ", u.ColumnNames.Select(col => $"[{col}]"))})";
            }));

            return results;
        }

        private IEnumerable<string> PrimaryKeyColumns(bool markedOnly = false)
        {
            return PrimaryKeyColumns(_modelType, markedOnly);
        }
    
        internal string PrimaryKeyColumnSyntax()
        {
            return string.Join(", ", PrimaryKeyColumns().Select(col => $"[{col}]"));
        }

        public override bool Equals(object obj)
        {
            Type t = obj as Type;
            if (t != null)
            {
                string schema, name;
                ParseNameAndSchema(t, out schema, out name);
                return Schema.Equals(schema) && Name.Equals(name);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Schema}.{Name}";
        }
    }
}
