using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Action
{
    public class AlterForeignKey : MergeForeignKeyBase
    {
        public AlterForeignKey(PropertyInfo propertyInfo, string description) : base(propertyInfo, MergeActionType.Alter, $"{propertyInfo.ForeignKeyName()}: {description}", nameof(AlterForeignKey))
        {
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;

            yield return $"ALTER TABLE {DbObject.SqlServerName(_pi.DeclaringType)} DROP CONSTRAINT [{_pi.ForeignKeyName()}]";

            ForeignKeyAttribute fk = _pi.GetForeignKeyAttribute();
            var obj = DbObject.FromType(_pi.DeclaringType);
            if (!fk.CreateIndex) yield return $"DROP INDEX [{_pi.IndexName()}] ON {obj}";

            foreach (var cmd in base.SqlCommands(connection)) yield return cmd;
        }
    }
}