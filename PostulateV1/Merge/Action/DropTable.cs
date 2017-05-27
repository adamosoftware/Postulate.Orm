using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Action
{
    public class DropTable : MergeAction
    {
        private readonly DbObject _object;        

        public DropTable(DbObject @object) : base(MergeObjectType.Table, MergeActionType.Drop, $"Drop table {@object}", nameof(DropTable))
        {            
            _object = @object;            
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var cmd in connection.GetFKDropStatements(_object.ObjectId)) yield return cmd;

            yield return $"DROP TABLE [{_object.Schema}].[{_object.Name}]";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            if (!_object.IsEmpty(connection))
            {
                yield return $"Cannot drop table {_object} because it's not empty.";
            }
        }

        public override string ToString()
        {
            return $"{_object.Schema}.{_object.Name}";
        }
    }
}
