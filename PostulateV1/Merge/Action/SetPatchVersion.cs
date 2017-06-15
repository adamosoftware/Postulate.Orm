using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Merge.Action
{
    public class SetPatchVersion : MergeAction
    {
        public const string MetaSchema = "meta";
        public const string TableName = "Patch";

        private readonly int _version;

        public SetPatchVersion(int version) : base(MergeObjectType.Metadata, MergeActionType.Create, $"Set schema version {version}", nameof(SetPatchVersion))
        {
            _version = version;
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {            
            if (!connection.Exists("[sys].[schemas] WHERE [name]=@name", new { name = MetaSchema })) yield return $"CREATE SCHEMA [{MetaSchema}]";            

            if (!connection.TableExists(MetaSchema, TableName))
            {
                yield return 
                    $@"CREATE TABLE [{MetaSchema}].[{TableName}] (
                        [Version] int NOT NULL,
                        [DateTime] datetime NOT NULL DEFAULT (getutcdate()),
                        CONSTRAINT [PK_{MetaSchema}_{TableName}] PRIMARY KEY ([Version])
                    )";
            }

            if (_version > 0)
            {
                yield return $@"
                    MERGE INTO [{MetaSchema}].[{TableName}] AS [target]
                    USING (SELECT {_version}) AS [source] ([Version])
                    ON [target].[Version]=[source].[Version]
                    WHEN NOT MATCHED THEN INSERT ([Version]) VALUES ([Version]);";
            }
        }

        public override IEnumerable<string> ValidationErrors(IDbConnection connection)
        {
            return new string[] { };
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
