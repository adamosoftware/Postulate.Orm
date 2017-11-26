using Postulate.Orm.Abstract;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AlterColumn : MergeAction
    {
        private readonly string _columnName;
        private readonly ColumnInfo _fromColumn;
        private readonly PropertyInfo _toColumn;
        private readonly TableInfo _affectedTable;

        public AlterColumn(SqlSyntax syntax, ColumnInfo fromColumn, PropertyInfo toColumn) : base(syntax, ObjectType.Column, ActionType.Alter, $"{toColumn.QualifiedName()}")
        {
            _columnName = toColumn.SqlColumnName();
            _fromColumn = fromColumn;
            _affectedTable = Syntax.GetTableInfoFromType(toColumn.ReflectedType);
            _toColumn = toColumn;
        }        

        public string ColumnName
        {
            get { return _columnName; }
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            string pkName;
            bool clustered;
            List<AddForeignKey> rebuildFKs = new List<AddForeignKey>();
            bool rebuildPK = Syntax.IsColumnInPrimaryKey(connection, _fromColumn, out clustered, out pkName);
            if (rebuildPK)
            {
                foreach (var fk in Syntax.GetDependentForeignKeys(connection, _affectedTable))
                {
                    rebuildFKs.Add(new AddForeignKey(Syntax, _toColumn));
                    yield return Syntax.ForeignKeyDropStatement(fk);
                }

                yield return Syntax.PrimaryKeyDropStatement(_affectedTable, pkName);
            }

            yield return Syntax.ColumnAlterStatement(_affectedTable, _toColumn);

            if (rebuildPK)
            {
                foreach (var fk in rebuildFKs)
                {
                    foreach (var cmd in fk.SqlCommands(connection)) yield return cmd;
                }

                yield return Syntax.PrimaryKeyAddStatement(_affectedTable);
            }
        }
    }
}