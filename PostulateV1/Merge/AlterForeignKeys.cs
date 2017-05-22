using Postulate.Orm.Merge.Action;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Dapper;
using System.Reflection;
using System.Text;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb>
    {
        /// <summary>
        /// Updates foreign keys whose cascade or indexing option has changed
        /// </summary>
        private IEnumerable<MergeAction> AlterForeignKeys(IDbConnection connection)
        {
            List<MergeAction> results = new List<MergeAction>();

            Dictionary<string, ForeignKeyAlterInfo> modelFKcascade = GetModelFKAlterInfo();
            Dictionary<string, ForeignKeyAlterInfo> schemaFKcascade = GetSchemaFKAlterInfo(connection);

            var changes = from m in modelFKcascade
                          join s in schemaFKcascade on m.Key equals s.Key
                          where !m.Value.Equals(s.Value)
                          select new { PropertyInfo = m.Value.PropertyInfo, ModelFK = m.Value, SchemaFK = s.Value };

            results.AddRange(changes.Select(item => new AlterForeignKey(item.PropertyInfo, item.SchemaFK - item.ModelFK)));

            return results;
        }

        private Dictionary<string, ForeignKeyAlterInfo> GetSchemaFKAlterInfo(IDbConnection connection)
        {
            return connection.Query<ForeignKeyAlterInfo>(
                @"SELECT 
                    [name] AS [ConstraintName],
                    CONVERT(bit, [delete_referential_action]) AS [IsCascadeDelete],        
                    CONVERT(bit, CASE 
                        WHEN EXISTS((SELECT 1 FROM [sys].[indexes] WHERE [name]='IX_' + SUBSTRING([fk].[name], 4, LEN([fk].[name])-2))) THEN 1
                        ELSE 0 
                    END) AS [IsIndexed]
                FROM 
                    [sys].[foreign_keys] [fk]").ToDictionary(
                        row => row.ConstraintName, 
                        row => new ForeignKeyAlterInfo() { IsCascadeDelete = row.IsCascadeDelete, IsIndexed = row.IsIndexed });
        }

        private Dictionary<string, ForeignKeyAlterInfo> GetModelFKAlterInfo()
        {
            var temp = _modelTypes.SelectMany(t => t.GetModelForeignKeys());

            return _modelTypes.SelectMany(t => t.GetModelForeignKeys())
                .ToDictionary(
                    item => item.ForeignKeyName(), 
                    item =>
                    {
                        ForeignKeyAttribute attr = item.GetForeignKeyAttribute();
                        return new ForeignKeyAlterInfo() { IsCascadeDelete = attr.CascadeDelete, IsIndexed = attr.CreateIndex, PropertyInfo = item };
                    });
        }
    }

    internal class ForeignKeyAlterInfo
    {
        public string ConstraintName { get; set; }
        public bool IsCascadeDelete { get; set; }
        public bool IsIndexed { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public override int GetHashCode()
        {
            return IsCascadeDelete.GetHashCode() + IsIndexed.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ForeignKeyAlterInfo ai = obj as ForeignKeyAlterInfo;
            if (ai != null)
            {
                return 
                    ai.IsIndexed == this.IsIndexed && 
                    ai.IsCascadeDelete == this.IsCascadeDelete;
            }
            return false;
        }

        public static string operator-(ForeignKeyAlterInfo from, ForeignKeyAlterInfo to)
        {            
            if (from.Equals(to)) return "No change";

            List<string> changes = new List<string>();
            if (from.IsCascadeDelete != to.IsCascadeDelete) changes.Add($"cascadeDelete:{to.IsCascadeDelete}");
            if (from.IsIndexed != to.IsIndexed) changes.Add($"indexed:{to.IsIndexed}");

            return string.Join(", ", changes);
        }
    }
}
