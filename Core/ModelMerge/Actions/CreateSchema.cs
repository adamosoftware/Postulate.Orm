using Postulate.Orm.Abstract;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.ModelMerge.Actions
{
	public class CreateSchema : MergeAction
	{
		private readonly string _schema;

		public CreateSchema(SqlSyntax syntax, string schema) : base(syntax, ObjectType.Schema, ActionType.Create, schema)
		{
			_schema = schema;
		}

		public override IEnumerable<string> SqlCommands(IDbConnection connection)
		{
			if (!Syntax.SchemaExists(connection, _schema))
			{
				yield return Syntax.CreateSchemaStatement(_schema);
			}
		}
	}
}