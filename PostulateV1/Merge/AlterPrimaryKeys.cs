using Postulate.Orm.Abstract;
using Postulate.Orm.Merge.Action;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Postulate.Orm.Extensions;
using Dapper;

namespace Postulate.Orm.Merge
{
    public partial class SchemaMerge<TDb>
    {
        /// <summary>
        /// Drops and rebuilds primary keys when included columns have changed
        /// </summary>
        private IEnumerable<MergeAction> AlterPrimaryKeys(IDbConnection connection)
        {
            var schemaPKColumns = GetSchemaPKColumns(connection).ToLookup(item => item.DbObject);
            var modelPKColumns = GetModelPKColumns().ToLookup(item => item.DbObject);

            List<MergeAction> results = new List<MergeAction>();

            var alteredKeys = from mk in modelPKColumns
                              join sk in schemaPKColumns on mk.Key equals sk.Key
                              where !Enumerable.SequenceEqual(mk, sk) || mk.Key.IsClusteredPK != sk.Key.IsClusteredPK
                              select mk;

            results.AddRange(alteredKeys.Select(pk => new AlterPrimaryKey(pk)));

            return results;
        }

        private IEnumerable<ColumnRef> GetModelPKColumns()
        {
            return _modelTypes
                .SelectMany(t => t.GetPrimaryKeyProperties())                
                .Select(pi => new ColumnRef(pi));
        }

        private IEnumerable<ColumnRef> GetSchemaPKColumns(IDbConnection connection)
        {
            var pkCols = connection.Query<PKColumnInfo>(
                @"SELECT
                    SCHEMA_NAME([t].[schema_id]) AS [Schema],
                    [t].[name] AS [TableName],
                    [col].[name] AS [ColumnName],
                    CONVERT(bit, CASE [pk].[type]
                        WHEN 1 THEN 1
                        ELSE 0
                    END) AS [IsClustered]
                FROM 
                    [sys].[indexes] [pk] INNER JOIN [sys].[index_columns] [pkcol] ON 
                        [pk].[index_id]=[pkcol].[index_id] AND
                        [pk].[object_id]=[pkcol].[object_id]
                    INNER JOIN [sys].[columns] [col] ON
                        [pkcol].[object_id]=[col].[object_id] AND
                        [pkcol].[column_id]=[col].[column_id]
                    INNER JOIN [sys].[tables] [t] ON [pk].[object_id]=[t].[object_id]
                WHERE 
                    [pk].[is_primary_key]=1
                ORDER BY
                    SCHEMA_NAME([t].[schema_id]),
                    [t].[name],
                    [col].[name]", null);

            return pkCols.Select(col => new ColumnRef()
                {
                    Schema = col.Schema,
                    TableName = col.TableName,
                    ColumnName = col.ColumnName,
                    DbObject = new DbObject(col.Schema, col.TableName) {  ModelType = FindModelType(col.Schema, col.TableName), IsClusteredPK = col.IsClustered }
                });
        }
    }

    internal class PKColumnInfo
    {
        public string Schema { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public bool IsClustered { get; set; }
    }
}
