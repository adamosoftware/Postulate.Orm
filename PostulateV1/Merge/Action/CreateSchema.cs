using Postulate.Orm.Extensions;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Action
{
    public class CreateSchema : MergeAction
    {
        private readonly string _schemaName;

        public CreateSchema(string schemaName) : base(MergeObjectType.Table, MergeActionType.Create, $"Create schema {schemaName}", nameof(CreateSchema))
        {
            _schemaName = schemaName;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            if (!connection.SchemaExists(_schemaName))
            {
                yield return $"CREATE SCHEMA [{_schemaName}]";
            }
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }
    }
}
