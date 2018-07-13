using Postulate.Orm.Abstract;
using Postulate.Orm.ModelMerge;
using Postulate.Orm.Models;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.SchemaMerge.Actions
{
	public class CreateTable : MergeAction
	{
		private readonly TableInfo _tableInfo;

		public CreateTable(SqlSyntax syntax, TableInfo table) : base(syntax, ObjectType.Table, ActionType.Create, table.ToString())
		{
			_tableInfo = table;
		}

		public TableInfo TableInfo { get { return _tableInfo; } }

		public override IEnumerable<string> SqlCommands(IDbConnection connection)
		{
			yield return Syntax.TableCreateStatement(connection, _tableInfo);
		}
	}
}