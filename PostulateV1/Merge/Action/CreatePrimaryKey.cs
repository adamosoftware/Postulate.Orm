using Postulate.Orm.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge.Action
{
    public class CreatePrimaryKey : MergeAction
    {
        private readonly DbObject _object;

        public CreatePrimaryKey(DbObject @object) : base(MergeObjectType.Key, MergeActionType.Create, $"Creating primary key on {@object}")
        {
            _object = @object;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;

            var modelType = _object.ModelType;

            var ct = new CreateTable(modelType);

            ClusterAttribute clusterAttribute =
                modelType.GetCustomAttribute<ClusterAttribute>() ??
                new ClusterAttribute(ClusterOption.PrimaryKey);

            yield return $"ALTER TABLE [{_object.Schema}].[{_object.Name}] ADD {ct.CreateTablePrimaryKey(clusterAttribute)}";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }
    }
}
