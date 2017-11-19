using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Merge.Actions;
using Postulate.Orm.Models;
using System.Data;

namespace Postulate.Orm.Util
{
    public static class Query
    {
        public static void SaveTrace(IDbConnection connection, QueryTrace trace, SqlDb<int> db)
        {
            if (!db.Syntax.TableExists(connection, typeof(QueryTrace)))
            {
                CreateTable ct = new CreateTable(db.Syntax, db.Syntax.GetTableInfoFromType(typeof(QueryTrace)));
                foreach (var cmd in ct.SqlCommands(connection)) connection.Execute(cmd);
            }

            db.Save(connection, trace);
        }
    }
}