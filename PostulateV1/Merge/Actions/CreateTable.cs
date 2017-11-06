using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class CreateTable : MergeAction
    {
        private readonly Type _modelType;
        private readonly bool _rebuild;

        public CreateTable(SqlSyntax scriptGen, Type modelType, bool rebuild = false) : base(scriptGen, ObjectType.Table, ActionType.Create, $"Create table {modelType.Name}")
        {
            _modelType = modelType;
            _rebuild = rebuild;
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
                var drop = new DropTable(this.Syntax, _modelType, connection);
                foreach (var cmd in drop.SqlCommands(connection)) yield return cmd;
            }

            yield return
                $"CREATE TABLE {Syntax.GetTableName(_modelType)} (\r\n\t" +
                    string.Join(",\r\n\t", CreateTableMembers()) +
                "\r\n)";
        }

        private string[] CreateTableMembers()
        {
            List<string> results = new List<string>();

            ClusterAttribute clusterAttribute = GetClusterAttribute();

            results.AddRange(CreateTableColumns());

            //results.Add(CreateTablePrimaryKey(clusterAttribute));

            //results.AddRange(CreateTableUniqueConstraints(clusterAttribute));

            return results.ToArray();
        }

        private ClusterAttribute GetClusterAttribute()
        {
            return _modelType.GetCustomAttribute<ClusterAttribute>() ?? new ClusterAttribute(ClusterOption.PrimaryKey);
        }

        private IEnumerable<string> CreateTableColumns()
        {
            List<string> results = new List<string>();

            Position identityPos = Position.StartOfTable;
            var ip = _modelType.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip == null) ip = _modelType.BaseType.GetCustomAttribute<IdentityPositionAttribute>();
            if (ip != null) identityPos = ip.Position;

            if (identityPos == Position.StartOfTable) results.Add(IdentityColumnSql());

            results.AddRange(ColumnProperties().Select(pi =>
            {
                string result = Syntax.GetColumnSyntax(pi);
                if (AddedColumns?.Contains(pi.SqlColumnName()) ?? false) result += " /* added */";
                return result;
            }));

            if (identityPos == Position.EndOfTable) results.Add(IdentityColumnSql());

            return results;
        }

        public IEnumerable<PropertyInfo> ColumnProperties()
        {
            return _modelType.GetProperties()
                .Where(p =>
                    p.CanWrite &&
                    !p.Name.ToLower().Equals(nameof(Record<int>.Id).ToLower()) &&
                    p.IsSupportedType(Syntax) &&
                    !p.HasAttribute<NotMappedAttribute>());
        }

        private string IdentityColumnSql()
        {
            Type keyType = FindKeyType(_modelType);

            return $"{Syntax.ApplyDelimiter(_modelType.IdentityColumnName())} {Syntax.KeyTypeMap()[keyType]}";
        }

        private Type FindKeyType(Type modelType)
        {
            if (!modelType.IsDerivedFromGeneric(typeof(Record<>))) throw new ArgumentException("Model class must derive from Record<TKey>");

            Type checkType = modelType;
            while (!checkType.IsGenericType) checkType = checkType.BaseType;
            return checkType.GetGenericArguments()[0];
        }
    }
}