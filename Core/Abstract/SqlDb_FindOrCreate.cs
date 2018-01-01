using System.Data;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey>
    {
        /// <summary>
        /// Finds an existing record or returns a default instance
        /// </summary>
        /// <typeparam name="TRecord"></typeparam>
        /// <param name="criteria">Filter expression</param>
        /// <param name="instance">Instance to return if record doesn't exist</param>
        public TRecord FindOrCreate<TRecord>(string criteria, TRecord instance, bool saveNewInstance = false) where TRecord : Record<TKey>, new()
        {
            using (var cn = GetConnection())
            {
                cn.Open();
                return FindOrCreate(cn, criteria, instance, saveNewInstance);
            }
        }

        public TRecord FindOrCreate<TRecord>(IDbConnection connection, string criteria, TRecord instance, bool saveNewInstance = false) where TRecord : Record<TKey>, new()
        {
            var result = FindWhere<TRecord>(connection, criteria, instance);
            if (result == null)
            {
                if (saveNewInstance) Save(connection, instance);
                return instance;
            }

            return result;
        }
    }
}