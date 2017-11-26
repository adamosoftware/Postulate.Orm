using Postulate.Orm;
using Postulate.Orm.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing.Models;

namespace Testing.Queries.MySql
{
    public class SingleCustomer : Query<Customer>
    {
        /// <summary>
        /// normally you would not write a query to select by id -- instead use <see cref="SqlDb{TKey}.Find{TRecord}(TKey)"/>
        /// </summary>
        /// <param name="db"></param>
        public SingleCustomer(SqlDb<int> db) : base("SELECT * FROM `customer` WHERE `id`=@id", db)
        {
        }

        public int Id { get; set; }
    }
}
