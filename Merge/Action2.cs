using Postulate.Orm.Merge.Enums;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge
{
    /// <summary>
    /// todo: rename to "Action" after refactoring out from Crud project
    /// </summary>
    public abstract class Action2
    {
        private readonly ObjectType _objectType;
        private readonly ActionType _actionType;
        private readonly string _description;

        public Action2(ObjectType objectType, ActionType actionType, string description)
        {
            _objectType = objectType;
            _actionType = actionType;
            _description = description;            
        }

        public ObjectType ObjectType { get { return _objectType; } }
        public ActionType ActionType { get { return _actionType; } }
        public string Description { get { return _description; } }        

        public virtual IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return Enumerable.Empty<string>();
        }

        public bool IsValid(IDbConnection connection)
        {
            return !ValidationErrors(connection).Any();
        }

        public virtual IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            yield return $"-- {Description}";
        }

        public override string ToString()
        {
            return Description;
        }
    }
}