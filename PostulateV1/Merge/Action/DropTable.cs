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

        public DropTable(DbObject @object) : base(MergeObjectType.Table, MergeActionType.DropAndCreate, $"Drop and create table {@object}")
        {            
            _object = @object;            
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {            
            var foreignKeys = connection.GetReferencingForeignKeys(_object.ObjectId);
            foreach (var fk in foreignKeys)
            {
                if (connection.Exists("[sys].[foreign_keys] WHERE [name]=@name", new { name = fk.ConstraintName }))
                {
                    yield return $"ALTER TABLE [{fk.ReferencingTable.Schema}].[{fk.ReferencingTable.Name}] DROP CONSTRAINT [{fk.ConstraintName}]";
                }
            }

            yield return $"DROP TABLE [{_object.Schema}].[{_object.Name}]";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            if (!_object.IsEmpty(connection))
            {
                yield return $"Cannot drop and rebuild {_object} because it's not empty.";
            }
        }
    }
}
