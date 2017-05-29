using Dapper;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ReflectionHelper;
using Postulate.Orm.Attributes;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        public TRecord Copy<TRecord>(TKey sourceId, object setProperties, params string[] omitColumns) where TRecord : Record<TKey>
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return Copy<TRecord>(cn, sourceId, setProperties, omitColumns);
            }
        }

        public TRecord Copy<TRecord>(IDbConnection connection, TKey sourceId, object setProperties, params string[] omitColumns) where TRecord : Record<TKey>
        {
            TKey newId = ExecuteCopy<TRecord>(connection, sourceId, setProperties, omitColumns);
            return ExecuteFind<TRecord>(connection, newId);
        }

        private TKey ExecuteCopy<TRecord>(IDbConnection connection, TKey id, object parameters, IEnumerable<string> omitColumns) where TRecord : Record<TKey>
        {
            string cmd = GetCopyStatement<TRecord>(parameters, omitColumns);
            DynamicParameters dp = new DynamicParameters(parameters);
            dp.Add(typeof(TRecord).IdentityColumnName(), id);
            return connection.QuerySingle<TKey>(cmd, dp);
        }

        private string GetCopyStatement<TRecord>(object parameters, IEnumerable<string> omitColumns) where TRecord : Record<TKey>
        {
            var paramColumns = parameters.GetType().GetProperties().Select(pi => pi.Name);

            var columns = GetColumnNames<TRecord>(pi =>
                    !pi.HasAttribute<CalculatedAttribute>()) // can't insert into calculated columns
                .Where(s =>
                    !s.Equals(typeof(TRecord).IdentityColumnName()) && // can't insert into identity column
                    (!omitColumns?.Select(omitCol => omitCol.ToLower()).Contains(s.ToLower()) ?? true) &&
                    !paramColumns.Select(paramCol => paramCol.ToLower()).Contains(s.ToLower())) // don't insert into param columns because we're providing new values
                .Select(colName => ApplyDelimiter(colName));

            return
                $@"INSERT INTO {GetTableName<TRecord>()} (
                    {string.Join(", ", columns.Concat(paramColumns.Select(col => ApplyDelimiter(col))))}
                ) OUTPUT
                    [inserted].[{typeof(TRecord).IdentityColumnName()}]
                SELECT
                    {string.Join(", ", columns.Concat(paramColumns.Select(col => $"@{col}")))}
                FROM
                    {GetTableName<TRecord>()}
                WHERE
                    [{typeof(TRecord).IdentityColumnName()}]=@id";
        }
    }
}
