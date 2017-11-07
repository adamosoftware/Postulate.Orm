using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Merge.Actions
{
    public class CreateTable : MergeAction
    {
        private readonly Type _modelType;
        private readonly bool _rebuild;

        public CreateTable(SqlSyntax syntax, Type modelType, bool rebuild = false) : base(syntax, ObjectType.Table, ActionType.Create, $"Create table {modelType.Name}")
        {
            _modelType = modelType;
            _rebuild = rebuild;
        }

        /// <summary>
        /// For rebuilt tables, enables generated script to indicate (using comments) which columns were added
        /// </summary>
        public IEnumerable<string> AddedColumns { get; set; }

        /// <summary>
        /// For rebuilt tables, enables generated script to indicate (using comments) which columns were modified
        /// </summary>
        public IEnumerable<string> ModifiedColumns { get; set; }

        /// <summary>
        /// For rebuilt tables, enables generated script to indicate (using comments) which columns were dropped
        /// </summary>
        public IEnumerable<string> DeletedColumns { get; set; }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            if (_rebuild)
            {
                var drop = new DropTable(this.Syntax, _modelType, connection);
                foreach (var cmd in drop.SqlCommands(connection)) yield return cmd;
            }

            yield return Syntax.GetCreateTableStatement(_modelType, AddedColumns, ModifiedColumns, DeletedColumns);            
        }
    }
}