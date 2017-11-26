using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Actions
{
    public class DropColumn : MergeAction
    {
        private readonly ColumnInfo _columnInfo;

        public DropColumn(SqlSyntax syntax, ColumnInfo columnInfo) : base(syntax, ObjectType.Column, ActionType.Drop, $"{columnInfo}")
        {
            _columnInfo = columnInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            string constrainName;
            bool clustered;
            bool inPK = Syntax.IsColumnInPrimaryKey(connection, _columnInfo, out clustered, out constrainName);
            var foreignKeys = Syntax.GetDependentForeignKeys(connection, _columnInfo.GetTableInfo()); ;

            if (inPK)
            {                
                foreach (var fk in foreignKeys) yield return Syntax.ForeignKeyDropStatement(fk);

                yield return $"ALTER TABLE [{_columnInfo.Schema}].[{_columnInfo.TableName}] DROP CONSTRAINT [{constrainName}]";
            }

            yield return $"ALTER TABLE [{_columnInfo.Schema}].[{_columnInfo.TableName}] DROP COLUMN [{_columnInfo.TableName}]";

            if (inPK)
            {
                yield return $"ALTER TABLE [{_columnInfo.Schema}].[{_columnInfo.TableName}] ADD CONSTRAINT [{constrainName}] PRIMARY KEY ({string.Join(", ", SqlSyntax.PrimaryKeyColumns(_columnInfo.PropertyInfo.ReflectedType))})";

                foreach (var fk in foreignKeys) yield return Syntax.ForeignKeyAddStatement(fk);
            }
        }
    }
}