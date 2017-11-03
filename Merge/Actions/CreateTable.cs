using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using Postulate.Orm.Merge.Enums;
using Postulate.Orm.Merge.Models;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class CreateTable : Action2
    {
        private readonly Type _modelType;
        private readonly bool _rebuild;        

        public CreateTable(Type modelType, bool rebuild = false) : base(ObjectType.Table, ActionType.Create, $"Create table {modelType.Name}")
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
            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;

            if (_rebuild)
            {
                var drop = new DropTable(_modelType, connection);
                foreach (var cmd in drop.SqlCommands(connection)) yield return cmd;
            }

            yield return
                $"CREATE TABLE {GetTableName()} (\r\n\t" +
                    string.Join(",\r\n\t", CreateTableMembers()) +
                "\r\n)";
        }

        public virtual string GetTableName()
        {
            var tableInfo = TableInfo.FromModelType(_modelType, Engine.DefaultSchema);
            return $"[{tableInfo.Schema}].[{tableInfo.Name}]";
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
                string result = pi.SqlColumnSyntax();
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
                    p.IsSupportedType() &&
                    !p.HasAttribute<NotMappedAttribute>());
        }

        private string IdentityColumnSql()
        {
            Type keyType = FindKeyType(_modelType);

            return $"[{_modelType.IdentityColumnName()}] {KeyTypeMap()[keyType]}";
        }

        private Type FindKeyType(Type modelType)
        {
            if (!modelType.IsDerivedFromGeneric(typeof(Record<>))) throw new ArgumentException("Model class must derive from Record<TKey>");

            Type checkType = modelType;
            while (!checkType.IsGenericType) checkType = checkType.BaseType;
            return checkType.GetGenericArguments()[0];
        }

        public virtual Dictionary<Type, string> KeyTypeMap(bool withDefaults = true)
        {
            return new Dictionary<Type, string>()
            {
                { typeof(int), $"int{((withDefaults) ? " identity(1,1)" : string.Empty)}" },
                { typeof(long), $"bigint{((withDefaults) ? " identity(1,1)" : string.Empty)}" },
                { typeof(Guid), $"uniqueidentifier{((withDefaults) ? " DEFAULT NewSequentialID()" : string.Empty)}" }
            };
        }
    }
}