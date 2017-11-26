using Dapper;
using Postulate.Orm.Abstract;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge
{
    public enum ActionType
    {
        Create,
        Alter,
        Rename,
        Drop,
        DropAndCreate
    }

    public enum ObjectType
    {
        Table,
        Column,
        Key,
        Index,
        ForeignKey,
        Metadata
    }

    public abstract class MergeAction
    {
        private readonly SqlSyntax _syntax;
        private readonly ObjectType _objectType;
        private readonly ActionType _actionType;
        private readonly string _description;

        public ObjectType ObjectType { get { return _objectType; } }
        public ActionType ActionType { get { return _actionType; } }
        public string Description { get { return _description; } }

        protected SqlSyntax Syntax { get { return _syntax; } }

        public MergeAction(SqlSyntax syntax, ObjectType objectType, ActionType actionType, string description)
        {
            _syntax = syntax;
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

        public void Execute(IDbConnection connection)
        {
            foreach (var cmd in SqlCommands(connection)) connection.Execute(cmd);
        }
    }
}