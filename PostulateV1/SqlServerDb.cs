using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace Postulate
{
    public class SqlServerDb : SqlDb
    {
        public SqlServerDb(string connectionName, string userName = null) : base(connectionName)
        {
            UserName = userName;
        }

        protected override string DelimitName(string name)
        {
            return string.Join(".", name.Split('.').Select(s => $"[{s}]"));
        }

        public override IDbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}
