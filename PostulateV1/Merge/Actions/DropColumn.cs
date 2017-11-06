using System.Collections.Generic;
using System.Data;
using Postulate.Orm.Merge.Abstract;
using Postulate.Orm.Merge.Models;

namespace Postulate.Orm.Merge.MergeActions
{
    public class DropColumn : MergeAction
    {
        public DropColumn(SqlScriptGenerator scriptGen, ColumnInfo columnInfo) : base(scriptGen, ObjectType.Column, ActionType.Drop, $"Drop column {columnInfo.ToString()}")
        {
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}