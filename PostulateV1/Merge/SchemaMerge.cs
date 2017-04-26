using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using Postulate.Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Postulate.Merge
{
    public enum MergeActionType
    {
        Create,
        Alter,
        Rename,
        Drop
    }

    public enum MergeObjectType
    {
        Table,
        NonKeyColumn,
        Key,
        Index,
        ForeignKey
    }

    internal delegate IEnumerable<SchemaMerge<TDb, TKey>.Diff> GetSchemaDiffMethod<TDb, TKey>(IDbConnection connection) where TDb : SqlDb<TKey>, new();

    public partial class SchemaMerge<TDb, TKey> where TDb : SqlDb<TKey>, new()
    {
        private readonly IEnumerable<Type> _modelTypes;

        public SchemaMerge()
        {
            _modelTypes = typeof(TDb).Assembly.GetTypes()
                .Where(t =>
                    !t.Name.StartsWith("<>") &&         
                    t.Namespace.Equals(typeof(TDb).Namespace) &&
                    !t.HasAttribute<NotMappedAttribute>() &&
                    !t.IsAbstract &&
                    t.IsDerivedFromGeneric(typeof(Record<>)));
        }

        public IEnumerable<Diff> Compare()
        {
            List<Diff> results = new List<Diff>();
            var db = new TDb();
            using (IDbConnection cn = db.GetConnection())
            {
                cn.Open();

                GetSchemaDiffMethod<TDb, TKey>[] diffMethods = new GetSchemaDiffMethod<TDb, TKey>[]
                {
                    // create
                    CreateTables, CreateNonKeyColumns, CreatePrimaryKeys, CreateUniqueKeys, CreateIndexes, CreateForeignKeys,

                    // alter
                    AlterPrimaryKeys, AlterUniqueKeys, AlterClustering, AlterColumnTypes, AlterForeignKeys,

                    // drop
                    DropTables, DropNonKeyColumns, DropPrimaryKeys, DropUniqueKeys, DropIndexes
                };
                foreach (var method in diffMethods) results.AddRange(method.Invoke(cn));
            }

            return results;
        }

        private IEnumerable<Diff> DropPrimaryKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> DropUniqueKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> DropIndexes(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> DropNonKeyColumns(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> DropTables(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> AlterForeignKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> AlterColumnTypes(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> AlterClustering(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> AlterUniqueKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> AlterPrimaryKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> CreateIndexes(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> CreateUniqueKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> CreatePrimaryKeys(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Diff> CreateNonKeyColumns(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public abstract class Diff
        {
            private readonly MergeObjectType _objectType;
            private readonly MergeActionType _actionType;
            private readonly string _description;

            public Diff(MergeObjectType objectType, MergeActionType actionType, string description)
            {
                _objectType = objectType;
                _actionType = actionType;
                _description = description;
            }

            public MergeObjectType ObjectType { get { return _objectType; } }
            public MergeActionType ActionType { get { return _actionType; } }

        }
    }
}
