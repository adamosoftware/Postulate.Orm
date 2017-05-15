using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Postulate.Orm.Extensions;
using Postulate.Orm.Attributes;

namespace Postulate.Orm.Merge.Action
{
    public class DropColumn : MergeAction
    {
        private readonly ColumnRef _columnRef;
        private readonly Type _modelType;

        public DropColumn(ColumnRef columnRef, Type modelType) : base(MergeObjectType.Column, MergeActionType.Drop, $"Drop column {columnRef}")
        {
            _columnRef = columnRef;
            _modelType = modelType;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            CreateTable ct = new CreateTable(_modelType);
            string pkName;            
            bool inPK = ct.InPrimaryKey(_columnRef.ColumnName, out pkName);

            if (inPK) yield return $"ALTER TABLE [{_columnRef.Schema}].[{_columnRef.ColumnName}] DROP CONSTRAINT [{pkName}]";

            yield return $"ALTER TABLE [{_columnRef.Schema}].[{_columnRef.ColumnName}] DROP COLUMN [{_columnRef.ColumnName}]";

            if (inPK) yield return $"ALTER TABLE [{_columnRef.Schema}].[{_columnRef.TableName}] ADD {ct.CreateTablePrimaryKey(ct.GetClusterAttribute())}";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }
    }
}
