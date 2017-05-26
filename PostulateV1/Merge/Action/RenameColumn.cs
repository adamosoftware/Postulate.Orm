using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Postulate.Orm.Merge.Action
{
    public class RenameColumn : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;
        private readonly RenameFromAttribute _attr;

        public RenameColumn(PropertyInfo propertyInfo) : base(MergeObjectType.Column, MergeActionType.Rename, RenameInfo(propertyInfo))
        {
            _propertyInfo = propertyInfo;
            _attr = propertyInfo.GetAttribute<RenameFromAttribute>();
        }

        private static string RenameInfo(PropertyInfo propertyInfo)
        {
            RenameFromAttribute attr = propertyInfo.GetAttribute<RenameFromAttribute>();
            DbObject obj = DbObject.FromType(propertyInfo.DeclaringType);
            obj.SquareBraces = false;
            return $"{obj}.{attr.OldName} -> {propertyInfo.SqlColumnName()}";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            var tbl = DbObject.FromType(_propertyInfo.DeclaringType);
            tbl.SquareBraces = false;
            if (!connection.ColumnExists(tbl.Schema, tbl.Name, _attr.OldName))
            {
                yield return $"Can't rename from {tbl}.{_attr.OldName} -- column doesn't exist.";
            }
            
            if (_propertyInfo.SqlColumnName().Equals(_attr.OldName))
            {
                yield return $"Can't rename column to the same name.";
            }
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            DbObject obj = DbObject.FromType(_propertyInfo.DeclaringType);
            obj.SquareBraces = false;
            yield return $"EXEC sp_rename '{obj}.{_attr.OldName}', '{_propertyInfo.SqlColumnName()}', 'COLUMN'";
        }
    }
}
