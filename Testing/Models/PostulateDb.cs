using Postulate;
using Postulate.Abstract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Models
{
    public class PostulateDb : SqlServerDb
    {
        public PostulateDb() : base("PostulateTest", "adamo")
        {
        }

        public void Save<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<int>
        {
            Save<TRecord, int>(connection, record);
        }

        public void Delete<TRecord>(IDbConnection connection, int id) where TRecord : Record<int>
        {
            Delete<TRecord, int>(connection, id);
        }

        public void Delete<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<int>
        {
            Delete<TRecord, int>(connection, record);
        }
    }
}
