using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.ModelMerge.Actions
{
    public class CreateTable : MergeAction
    {
        private readonly TableInfo _tableInfo;
        private readonly Type _modelType;
        private readonly bool _rebuild;
        private readonly bool _withForeignKeys;

        public CreateTable(SqlSyntax syntax, TableInfo tableInfo, bool rebuild = false, bool withForeignKeys = false) : base(syntax, ObjectType.Table, ActionType.Create, $"{tableInfo}")
        {
            if (tableInfo.ModelType == null) throw new ArgumentException("CreateTable requires a TableInfo that has its ModelType property set.");

            _tableInfo = tableInfo;
            _modelType = tableInfo.ModelType;
            _rebuild = rebuild;
            _withForeignKeys = withForeignKeys;
        }

        /// <summary>
        /// For rebuilt tables, enables generated script to indicate (using comments) which columns were added
        /// </summary>
        public IEnumerable<string> AddedColumns { get; set; }

        /// <summary>
        /// For rebuilt tables, enables generated script to indicate (using comments) which columns were modified
        /// </summary>
        public IEnumerable<string> ModifiedColumns { get; set; }

        /// <summary>
        /// For rebuilt tables, enables generated script to indicate (using comments) which columns were dropped
        /// </summary>
        public IEnumerable<string> DeletedColumns { get; set; }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            if (_rebuild)
            {
                var drop = new DropTable(this.Syntax, _tableInfo);
                foreach (var cmd in drop.SqlCommands(connection)) yield return cmd;
            }
            
            yield return Syntax.TableCreateStatement(_modelType, AddedColumns, ModifiedColumns, DeletedColumns, _withForeignKeys);
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            if (_modelType.GetProperties().Any(pi => pi.HasAttribute<UniqueKeyAttribute>(attr => attr.IsClustered)) && _modelType.HasAttribute<ClusterAttribute>())
            {
                yield return "Model class with [Cluster] attribute may not have properties with a clustered unique key.";
            }

            foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<PrimaryKeyAttribute>())))
            {
                foreach (var err in GetPrimaryKeyValidationErrors(pi)) yield return err;
            }

            foreach (var pi in _modelType.GetProperties().Where(pi => (pi.HasAttribute<UniqueKeyAttribute>())))
            {
                if (Syntax.SqlDataType(pi).ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
            }

            // class-level unique with MAX
            var uniques = _modelType.GetCustomAttributes<UniqueKeyAttribute>();
            foreach (var u in uniques)
            {
                foreach (var col in u.ColumnNames)
                {
                    PropertyInfo pi = _modelType.GetProperty(col);
                    if (Syntax.SqlDataType(pi).ToLower().Contains("char(max)")) yield return $"Unique column [{pi.Name}] may not use MAX size.";
                }
            }
        }

        public IEnumerable<string> GetPrimaryKeyValidationErrors(PropertyInfo propertyInfo)
        {
            if (Syntax.SqlDataType(propertyInfo).ToLower().Contains("char(max)")) yield return $"Primary key column [{propertyInfo.Name}] may not use MAX size.";
            if (propertyInfo.PropertyType.IsNullableGeneric()) yield return $"Primary key column [{propertyInfo.Name}] may not be nullable.";
        }
    }
}