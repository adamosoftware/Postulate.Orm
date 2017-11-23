using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Action
{
    public class DropPrimaryKey : MergeAction
    {
        private readonly DbObject _object;

        public DropPrimaryKey(DbObject @object) : base(MergeObjectType.Key, MergeActionType.Drop, $"Drop primary key on {@object}", nameof(DropPrimaryKey))
        {
            _object = @object;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;

            foreach (var cmd in connection.GetFKDropStatements(_object.ObjectId)) yield return cmd;

            yield return $"ALTER TABLE [{_object.Schema}].[{_object.Name}] DROP CONSTRAINT [PK_{DbObject.ConstraintName(_object.ModelType)}]";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }
    }
}