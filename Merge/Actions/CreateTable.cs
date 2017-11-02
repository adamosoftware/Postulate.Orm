using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Postulate.Orm.Merge.Enum;

namespace Postulate.Orm.Merge.Actions
{
    public class CreateTable : Action2
    {
        private readonly Type _modelType;

        public CreateTable(Type modelType) : base(ObjectType.Table, ActionType.Create)
        {
            _modelType = modelType;
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
