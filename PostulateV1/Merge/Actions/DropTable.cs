using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge.Actions
{
    public class DropTable : MergeAction
    {
        private readonly TableInfo _tableInfo;

        public DropTable(SqlSyntax syntax, TableInfo tableInfo) : base(syntax, ObjectType.Table, ActionType.Drop, $"Drop table {tableInfo.ToString()}")
        {
            _tableInfo = tableInfo;
        }

        public DropTable(SqlSyntax scriptGen, Type modelType, IDbConnection connection = null) : this(scriptGen, TableInfo.FromModelType(modelType))
        {
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            if (_tableInfo.ObjectId == 0) throw new InvalidOperationException($"Can't drop dependent foreign keys on table {_tableInfo} because the ObjectId property is not set.");

            foreach (var fk in Syntax.GetDependentForeignKeys(connection, _tableInfo)) yield return Syntax.GetDropForeignKeyStatement(fk);

            yield return Syntax.GetDropTableStatement(_tableInfo);
        }
    }
}