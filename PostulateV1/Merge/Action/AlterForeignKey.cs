using Postulate.Orm.Extensions;
using System.Collections.Generic;
using System.Reflection;
using System.Data;
using Postulate.Orm.Attributes;

namespace Postulate.Orm.Merge.Action
{
    public class AlterForeignKey : MergeForeignKeyBase
    {        
        public AlterForeignKey(PropertyInfo propertyInfo, string description) : base(propertyInfo, MergeActionType.Alter, $"{propertyInfo.ForeignKeyName()}: {description}")
        {        
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            yield return $"ALTER TABLE {DbObject.SqlServerName(_pi.DeclaringType)} DROP CONSTRAINT [{_pi.ForeignKeyName()}]";

            ForeignKeyAttribute fk = _pi.GetForeignKeyAttribute();
            if (!fk.CreateIndex) yield return $"DROP INDEX [{_pi.IndexName()}]";

            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;
        }
    }
}
