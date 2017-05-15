using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge.Action
{
    public abstract class MergeAction
    {
        private readonly MergeObjectType _objectType;
        private readonly MergeActionType _actionType;
        private readonly string _description;
        private readonly string _scriptComment;

        public MergeAction(MergeObjectType objectType, MergeActionType actionType, string description, string scriptComment = null)
        {
            _objectType = objectType;
            _actionType = actionType;
            _description = description;
            _scriptComment = scriptComment;
        }

        public MergeObjectType ObjectType { get { return _objectType; } }
        public MergeActionType ActionType { get { return _actionType; } }
        public string Description { get { return _description; } }
        public string ScriptComment {  get { return _scriptComment; } }

        public abstract IEnumerable<string> ValidationErrors(IDbConnection connection);

        public bool IsValid(IDbConnection connection)
        {
            return !ValidationErrors(connection).Any();
        }

        public abstract IEnumerable<string> SqlCommands(IDbConnection connection);
    }
}
