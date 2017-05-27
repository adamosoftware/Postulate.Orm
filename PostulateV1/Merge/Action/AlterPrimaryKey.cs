using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Postulate.Orm.Merge.Action
{
    public class AlterPrimaryKey : MergeAction
    {
        private readonly IGrouping<DbObject, ColumnRef> _pk;

        public AlterPrimaryKey(IGrouping<DbObject, ColumnRef> pk) : base(MergeObjectType.Key, MergeActionType.Alter, $"Primary key {pk.Key}: {string.Join(", ", pk.Select(cr => cr.ColumnName))}")
        {
            _pk = pk;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;

            var referencingFKs = _pk.Key.GetReferencingForeignKeys(connection);

            foreach (var fk in referencingFKs) yield return $"ALTER TABLE [{fk.Child.Schema}].[{fk.Child.TableName}] DROP CONSTRAINT [{fk.ConstraintName}]";                        

            yield return $"ALTER TABLE [{_pk.Key.Schema}].[{_pk.Key.Name}] DROP CONSTRAINT [{_pk.Key.PKConstraintName}]";

            string clustering = (_pk.Key.IsClusteredPK) ? "CLUSTERED" : "NONCLUSTERED";
            yield return $"ALTER TABLE [{_pk.Key.Schema}].[{_pk.Key.Name}] ADD CONSTRAINT [{_pk.Key.PKConstraintName}] PRIMARY KEY {clustering} ({string.Join(", ", _pk.Select(cr => $"[{cr.ColumnName}]"))})";

            foreach (var fk in referencingFKs)
            {
                yield return $"ALTER TABLE [{fk.Child.Schema}].[{fk.Child.TableName}] ADD CONSTRAINT [{fk.ConstraintName}] FOREIGN KEY ([{fk.Child.ColumnName}]) REFERENCES ([{fk.Parent.ColumnName}])";
            }
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            foreach (var cr in _pk)
            {
                foreach (var err in cr.PropertyInfo.GetPrimaryKeyValidationErrors()) yield return err;                
            }

            if (!connection.IsTableEmpty(_pk.Key.Schema, _pk.Key.Name))
            {
                string dupQuery, keyExpression;
                if (HasDuplicates(connection, out keyExpression, out dupQuery)) yield return $"Can't create primary key because there are duplicates for key expression {keyExpression}. Verify with query: {dupQuery}";
            }
        }

        public override string ToString()
        {
            return Description;
        }

        private bool HasDuplicates(IDbConnection connection, out string keyExpression, out string dupQuery)
        {
            keyExpression = string.Join(", ", _pk.Select(cr => $"[{cr.ColumnName}]"));
            dupQuery = $"SELECT {keyExpression}, COUNT(1) AS [Count] FROM [{_pk.Key.Schema}].[{_pk.Key.Name}] GROUP BY {keyExpression} HAVING COUNT(1)>1";

            try
            {
                var results = connection.Query(dupQuery);
                return (results.Any());
            }
            catch 
            {
                // an exception here usually means the new key relies on columns that don't exist yet
                return false;
            }
        }
    }
}
