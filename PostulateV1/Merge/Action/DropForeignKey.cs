using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Action
{
    public class DropForeignKey : MergeAction
    {
        private readonly ForeignKeyRef _fk;

        public DropForeignKey(ForeignKeyRef fk) : base(MergeObjectType.ForeignKey, MergeActionType.Drop, $"Drop foreign key {fk.ConstraintName}", nameof(DropForeignKey))
        {
            _fk = fk;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;

            yield return $"ALTER TABLE [{_fk.ChildObject.Schema}].[{_fk.ChildObject.Name}] DROP CONSTRAINT [{_fk.ConstraintName}]";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }

        public override string ToString()
        {
            return _fk.ConstraintName;
        }
    }
}