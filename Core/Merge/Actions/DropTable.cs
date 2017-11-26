using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Actions
{
    public class DropTable : MergeAction
    {
        private readonly TableInfo _tableInfo;

        public DropTable(SqlSyntax syntax, TableInfo tableInfo) : base(syntax, ObjectType.Table, ActionType.Drop, $"{tableInfo}")
        {
            _tableInfo = tableInfo;
        }

        public DropTable(SqlSyntax syntax, Type modelType, IDbConnection connection = null) : this(syntax, syntax.GetTableInfoFromType(modelType))
        {
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            if (_tableInfo.ObjectId == 0) throw new InvalidOperationException($"Can't drop dependent foreign keys on table {_tableInfo} because the ObjectId property is not set.");

            foreach (var fk in Syntax.GetDependentForeignKeys(connection, _tableInfo)) yield return Syntax.ForeignKeyDropStatement(fk);

            yield return Syntax.TableDropStatement(_tableInfo);
        }
    }
}