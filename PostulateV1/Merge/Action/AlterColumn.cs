using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge.Action
{
    public class AlterColumn : MergeAction
    {
        private readonly ColumnRef _newColumn;
        private readonly ColumnRef _oldColumn;
        private readonly int _objectId;

        public AlterColumn(ColumnRef newColumn, ColumnRef oldColumn) : base(MergeObjectType.Column, MergeActionType.Alter, $"{newColumn.ToString()}: {ColumnRef.CompareSyntaxes(oldColumn, newColumn)}", nameof(AlterColumn))
        {
            _newColumn = newColumn;
            _oldColumn = oldColumn;
            _objectId = oldColumn.ObjectID; // this comes from GetSchemaColumns, so it has the ObjectID
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;

            // if pk column, drop referencing foreign keys, any covering indexes, and then the pk itself
            IEnumerable<ForeignKeyRef> referencingFKs = null;

            bool inPK = false;
            bool rebuildFKs = false;

            string pkConstraint; bool isClustered;
            CreateTable ct = new CreateTable(_newColumn.ModelType);
            if (_newColumn.InPrimaryKey(connection, out pkConstraint, out isClustered) || ct.InPrimaryKey(_newColumn.ColumnName, out pkConstraint))
            {
                inPK = true;
                referencingFKs = connection.GetReferencingForeignKeys(_objectId);
                if (referencingFKs.Any(fk => fk.Parent.ColumnName.Equals(_newColumn.ColumnName)))
                {
                    rebuildFKs = true;
                    foreach (var fk in referencingFKs) yield return $"ALTER TABLE [{fk.Child.Schema}].[{fk.Child.TableName}] DROP CONSTRAINT [{fk.ConstraintName}]";
                }

                // todo: indexes

                yield return $"ALTER TABLE [{_newColumn.DbObject.Schema}].[{_newColumn.DbObject.Name}] DROP CONSTRAINT [{pkConstraint}]";
            }

            // alter desired column
            bool withCollation = ColumnRef.IsCollationChanged(_oldColumn, _newColumn);
            yield return $"ALTER TABLE [{_newColumn.DbObject.Schema}].[{_newColumn.DbObject.Name}] ALTER COLUMN [{_newColumn.ColumnName}] {_newColumn.GetDataTypeSyntax(withCollation)}";

            // rebuild pk and foreign keys
            if (inPK)
            {
                string clustering = (isClustered) ? "CLUSTERED" : "NONCLUSTERED";
                yield return $"ALTER TABLE [{_newColumn.DbObject.Schema}].[{_newColumn.DbObject.Name}] ADD CONSTRAINT [{pkConstraint}] PRIMARY KEY {clustering} ({ct.PrimaryKeyColumnSyntax()})";

                if (rebuildFKs)
                {
                    foreach (var fk in referencingFKs)
                    {
                        yield return $"ALTER TABLE [{fk.Child.Schema}].[{fk.Child.TableName}] ADD CONSTRAINT [{fk.ConstraintName}] FOREIGN KEY ([{fk.Child.ColumnName}]) REFERENCES ([{fk.Parent.ColumnName}])";
                    }
                }
            }
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }

        public override string ToString()
        {
            return Description;
        }
    }
}