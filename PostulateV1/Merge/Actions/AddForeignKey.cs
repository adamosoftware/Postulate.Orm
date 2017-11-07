using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Models;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class AddForeignKey : MergeAction
    {
        private readonly PropertyInfo _propertyInfo;

        public AddForeignKey(SqlSyntax syntax, PropertyInfo propertyInfo) : base(syntax, ObjectType.ForeignKey, ActionType.Create, $"Add foreign key {propertyInfo.QualifiedName()}")
        {
            _propertyInfo = propertyInfo;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            // todo: move this to SqlSyntax abstract methods
            ForeignKeyAttribute fk = _propertyInfo.GetForeignKeyAttribute();
            string cascadeDelete = (fk.CascadeDelete) ? " ON DELETE CASCADE" : string.Empty;
            yield return
                $"ALTER TABLE {Syntax.GetTableName(_propertyInfo.DeclaringType)} ADD CONSTRAINT [{_propertyInfo.ForeignKeyName(Syntax)}] FOREIGN KEY (\r\n" +
                    $"\t[{_propertyInfo.SqlColumnName()}]\r\n" +
                $") REFERENCES {Syntax.GetTableName(fk.PrimaryTableType)} (\r\n" +
                    $"\t[{fk.PrimaryTableType.IdentityColumnName()}]\r\n" +
                ")" + cascadeDelete;

            if (fk.CreateIndex && !connection.Exists("[sys].[indexes] WHERE [name]=@name", new { name = _propertyInfo.IndexName(Syntax) }))
            {
                var obj = TableInfo.FromModelType(_propertyInfo.DeclaringType);
                yield return $"CREATE INDEX [{_propertyInfo.IndexName(Syntax)}] ON {Syntax.GetTableName(obj.ModelType)} ([{_propertyInfo.SqlColumnName()}])";
            }
        }
    }
}