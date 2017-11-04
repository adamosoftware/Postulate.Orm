using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge.Abstract
{
    public abstract class MergeAction
    {
        private readonly SqlScriptGenerator _scriptGen;
        private readonly ObjectType _objectType;
        private readonly ActionType _actionType;
        private readonly string _description;

        public ObjectType ObjectType { get { return _objectType; } }
        public ActionType ActionType { get { return _actionType; } }
        public string Description { get { return _description; } }

        protected SqlScriptGenerator SqlScriptGenerator { get { return _scriptGen; } }

        public MergeAction(SqlScriptGenerator scriptGenerator, ObjectType objectType, ActionType actionType, string description)
        {
            _scriptGen = scriptGenerator;
            _objectType = objectType;
            _actionType = actionType;
            _description = description;
        }

        public virtual IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return Enumerable.Empty<string>();
        }

        public bool IsValid(IDbConnection connection)
        {
            return !ValidationErrors(connection).Any();
        }

        public abstract IEnumerable<string> SqlCommands(IDbConnection connection);        

        public override string ToString()
        {
            return Description;
        }
    }
}