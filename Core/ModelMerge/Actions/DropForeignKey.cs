using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.ModelMerge.Actions
{
    public class DropForeignKey : MergeAction
    {
        private readonly ColumnInfo _columnInfo;

        public DropForeignKey(SqlSyntax syntax, ColumnInfo columnInfo) : base(syntax, ObjectType.ForeignKey, ActionType.Drop, columnInfo.ForeignKeyConstraint)
        {
            _columnInfo = columnInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            ForeignKeyInfo fk = _columnInfo.ToForeignKeyInfo();
            yield return Syntax.ForeignKeyDropStatement(fk);
        }
    }
}