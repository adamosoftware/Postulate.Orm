using Postulate.Orm.Merge.Enums;
using Postulate.Orm.Merge.Extensions;
using Postulate.Orm.Merge.Models;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class DropColumn : Action2
    {
        public DropColumn(ColumnInfo columnInfo) : base(ObjectType.Column, ActionType.Drop, $"Drop column {columnInfo.ToString()}")
        {
        }
    }
}