using Postulate.Orm.Merge.Enum;
using System;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Merge.Actions
{
    public class CreateTable : Action2
    {
        private readonly Type _modelType;
        private readonly bool _rebuild;

        public CreateTable(Type modelType, bool rebuild = false) : base(ObjectType.Table, ActionType.Create)
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

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}