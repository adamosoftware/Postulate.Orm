using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Postulate.Extensions;
using Postulate.Attributes;
using Dapper;

namespace Postulate.Merge.Action
{
    public class CreateForeignKey : MergeAction
    {
        private readonly PropertyInfo _pi;

        public CreateForeignKey(PropertyInfo propertyInfo) : base(MergeObjectType.ForeignKey, MergeActionType.Create, $"Add foreign key {propertyInfo.ForeignKeyName()}")            
        {
            _pi = propertyInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            ForeignKeyAttribute fk = _pi.GetForeignKeyAttribute();
            string cascadeDelete = (fk.CascadeDelete) ? " ON DELETE CASCADE" : string.Empty;
            yield return
                $"ALTER TABLE {DbObject.SqlServerName(_pi.DeclaringType)} ADD CONSTRAINT [{_pi.ForeignKeyName()}] FOREIGN KEY (\r\n" +
                    $"\t[{_pi.SqlColumnName()}]\r\n" +
                $") REFERENCES {DbObject.SqlServerName(fk.PrimaryTableType)} (\r\n" +
                    $"\t[{fk.PrimaryTableType.IdentityColumnName()}]\r\n" +
                ")" + cascadeDelete;

            if (fk.CreateIndex)
            {
                yield return $"CREATE INDEX [IX_{DbObject.SqlServerName(_pi.DeclaringType)}_{_pi.SqlColumnName()}] ([{_pi.SqlColumnName()}])";
            }
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }
    }
}
