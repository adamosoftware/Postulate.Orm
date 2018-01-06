using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.ModelMerge.Actions;
using Postulate.Orm.Models;
using System.Data;

namespace Postulate.Orm.Util
{
    public static class Query
    {
        /// <summary>
        /// Provides a general-purpose way to save QueryTraces. Use this in <see cref="Query{TResult}.TraceCallback"/> or
        /// <see cref="SqlDb{TKey}.TraceCallback"/> handlers
        /// </summary>        
        public static void SaveTrace(IDbConnection connection, QueryTrace trace, SqlDb<int> db)
        {
            if (!db.Syntax.TableExists(connection, typeof(QueryTrace)))
            {
                CreateTable ct = new CreateTable(db.Syntax, db.Syntax.GetTableInfoFromType(typeof(QueryTrace)));
                foreach (var cmd in ct.SqlCommands(connection)) connection.Execute(cmd);
            }

            var callback = db.TraceCallback;

            // prevent infinite loop
            db.TraceCallback = null;

            db.Save(connection, trace);

            // restore original callback
            db.TraceCallback = callback;
        }
    }
}