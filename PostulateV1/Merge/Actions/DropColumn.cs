using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Actions
{
    public class DropColumn : MergeAction
    {
        public DropColumn(SqlSyntax scriptGen, ColumnInfo columnInfo) : base(scriptGen, ObjectType.Column, ActionType.Drop, $"Drop column {columnInfo.ToString()}")
        {
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}