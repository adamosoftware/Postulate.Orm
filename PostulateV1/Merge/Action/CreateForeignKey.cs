using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using System.Reflection;

namespace Postulate.Orm.Merge.Action
{
    public class CreateForeignKey : MergeForeignKeyBase
    {
        public CreateForeignKey(PropertyInfo propertyInfo) : base(propertyInfo, MergeActionType.Create, $"Add foreign key {propertyInfo.ForeignKeyName()}", nameof(CreateForeignKey))
        {
        }

        public override string ToString()
        {
            ForeignKeyAttribute fk = _pi.GetForeignKeyAttribute();
            return $"{DbObject.SqlServerName(_pi.DeclaringType, false)}.{_pi.SqlColumnName()} -> {DbObject.SqlServerName(fk.PrimaryTableType, false)}.{fk.PrimaryTableType.IdentityColumnName()}";
        }
    }
}