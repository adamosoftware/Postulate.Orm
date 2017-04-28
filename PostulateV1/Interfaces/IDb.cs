using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Interfaces
{
    /// <summary>
    /// Enables SchemaMerge to work without SqlDb&lt;Key&gt;
    /// </summary>
    public interface IDb
    {
        int Version { get; }
        IDbConnection GetConnection();
    }
}
