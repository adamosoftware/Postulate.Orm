using Postulate.Orm.Abstract;
using System.Data;

namespace Postulate.Orm.Interfaces
{
    /// <summary>
    /// Enables SchemaMerge to work without SqlDb&lt;Key&gt;
    /// </summary>
    public interface IDb
    {
        int Version { get; }

        IDbConnection GetConnection();
        SqlSyntax Syntax { get; }

        string ConnectionName { get; }
    }
}