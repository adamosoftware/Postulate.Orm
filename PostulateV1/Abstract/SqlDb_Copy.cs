using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using ReflectionHelper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        public TRecord Copy<TRecord>(TKey sourceId, object setProperties, params string[] omitColumns) where TRecord : Record<TKey>, new()
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                return Copy<TRecord>(cn, sourceId, setProperties, omitColumns);
            }
        }

        public TRecord Copy<TRecord>(IDbConnection connection, TKey sourceId, object setProperties, params string[] omitColumns) where TRecord : Record<TKey>, new()
        {
            TKey newId = ExecuteCopy<TRecord>(connection, sourceId, setProperties, omitColumns);
            return ExecuteFind<TRecord>(connection, newId);
        }

        private TKey ExecuteCopy<TRecord>(IDbConnection connection, TKey id, object setProperties, IEnumerable<string> omitColumns) where TRecord : Record<TKey>
        {
            string cmd = GetCopyStatement<TRecord>(setProperties, omitColumns);
            DynamicParameters dp = new DynamicParameters(setProperties);
            dp.Add(typeof(TRecord).IdentityColumnName(), id);
            return connection.QuerySingle<TKey>(cmd, dp);
        }

        private string GetCopyStatement<TRecord>(object setProperties, IEnumerable<string> omitColumns) where TRecord : Record<TKey>
        {
            var paramColumns = setProperties.GetType().GetProperties().Select(pi => pi.Name);

            var columns = GetColumnNames<TRecord>(pi =>
                    !pi.HasAttribute<CalculatedAttribute>()) // can't insert into calculated columns
                .Where(s =>
                    !s.Equals(typeof(TRecord).IdentityColumnName()) && // can't insert into identity column
                    (!omitColumns?.Select(omitCol => omitCol.ToLower()).Contains(s.ToLower()) ?? true) &&
                    !paramColumns.Select(paramCol => paramCol.ToLower()).Contains(s.ToLower())) // don't insert into param columns because we're providing new values
                .Select(colName => Syntax.ApplyDelimiter(colName));

            return Syntax.GetCopyStatement<TRecord, TKey>(paramColumns, columns);
        }
    }
}