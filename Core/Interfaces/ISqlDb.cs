using Postulate.Orm.Abstract;
using System.Data;

namespace Postulate.Orm.Interfaces
{
	/// <summary>
	/// Enables SchemaMerge to work without <see cref="SqlDb{TKey}"/>
	/// </summary>
	public interface ISqlDb
	{
		int Version { get; }

		IDbConnection GetConnection();

		SqlSyntax Syntax { get; }

		string ConnectionName { get; }
		string UserName { get; }
	}
}