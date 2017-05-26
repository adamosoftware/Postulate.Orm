using Postulate.Orm.Attributes;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using Postulate.Orm.Extensions;
using System.Linq;
using Postulate.Orm.Abstract;

namespace Postulate.Orm.Merge.Action
{
    public class RenameTable : MergeAction
    {
        private readonly RenameFromAttribute _attr;
        private readonly Type _modelType;        

        public RenameTable(Type modelType) : base(MergeObjectType.Table, MergeActionType.Rename, RenameDescription(modelType))
        {
            _modelType = modelType;
            _attr = modelType.GetAttribute<RenameFromAttribute>();
        }

        public static string RenameDescription(Type modelType)
        {
            RenameFromAttribute attr = modelType.GetAttribute<RenameFromAttribute>();
            DbObject obj = DbObject.FromType(modelType);
            obj.SquareBraces = false;
            return $"{attr.OldName} -> {obj}";
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            var oldTable = DbObject.Parse(_attr.OldName);
            oldTable.SquareBraces = false;
            if (!connection.TableExists(oldTable.Schema, oldTable.Name))
            {
                yield return $"Can't rename from {oldTable} -- table doesn't exist.";
            }

            var newTable = DbObject.FromType(_modelType);
            if (oldTable.Equals(newTable))
            {
                yield return $"Can't rename table to the same name.";
            }

            if (_modelType.GetProperties().Any(pi => pi.HasAttribute<RenameFromAttribute>()))
            {
                yield return $"Can't rename columns while containing table is being renamed. Please do these changes one after the other.";
            }
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {            
            CreateTable ct = new CreateTable(_modelType);
            foreach (var cmd in ct.SqlCommands(connection)) yield return cmd;

            DbObject newTable = DbObject.FromType(_modelType);
            DbObject oldTable = DbObject.Parse(_attr.OldName);            

            if (!connection.IsTableEmpty(newTable.Schema, newTable.Name))
            {
                yield return $"SET IDENTITY_INSERT [{newTable.Schema}.[{newTable.Name}] ON";

                string columnNames = string.Join(", ", ct.ColumnProperties().Select(pi => $"[{pi.SqlColumnName()}]").Concat(new string[] { SqlDb<int>.IdentityColumnName }));
                yield return $"INSERT INTO [{newTable.Schema}].[{newTable.Name}] ({columnNames}) SELECT {columnNames} FROM [{oldTable.Schema}].[{oldTable.Name}]";
                
                yield return $"SET IDENTITY_INSERT [{newTable.Schema}.[{newTable.Name}] OFF";
            }
            
            DbObject.SetObjectId(connection, oldTable);
            foreach (var cmd in connection.GetFKDropStatements(oldTable.ObjectId)) yield return cmd;

            yield return $"DROP TABLE [{oldTable.Schema}].[{oldTable.Name}]";
        }
    }
}
