using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge.Action
{
    public class DropTable : MergeAction
    {
        private readonly DbObject _object;        

        public DropTable(DbObject @object, string scriptComment = null) : base(MergeObjectType.Table, MergeActionType.Drop, $"Drop table {@object}", scriptComment)
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
    }
}
