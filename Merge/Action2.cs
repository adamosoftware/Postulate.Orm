using Postulate.Orm.Merge.Enum;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge
{
    /// <summary>
    /// todo: rename to Action after refactoring out from Crud project
    /// </summary>
    public abstract class Action2
    {
        private readonly ObjectType _objectType;
        private readonly ActionType _actionType;
        private readonly string _description;
        private readonly string _sourceAction;
        private ObjectType table;
        private ActionType create;

        public Action2(ObjectType objectType, ActionType actionType, string description, string sourceAction)
        {
            _objectType = objectType;
            _actionType = actionType;
            _description = description;
            _sourceAction = sourceAction;
        }

        public Action2(ObjectType table, ActionType create)
        {
            this.table = table;
            this.create = create;
        }

        public ObjectType ObjectType { get { return _objectType; } }
        public ActionType ActionType { get { return _actionType; } }
        public string Description { get { return _description; } }
        public string SourceAction { get { return _sourceAction; } }

        public abstract IEnumerable<string> ValidationErrors(IDbConnection connection);

        public bool IsValid(IDbConnection connection)
        {
            return !ValidationErrors(connection).Any();
        }

        public virtual IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            yield return $"-- {Description} ({SourceAction})";
        }

        public override string ToString()
        {
            return Description;
        }
    }
}