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
        public TRecord FindOrCreate<TRecord>(string criteria, TRecord instance) where TRecord : Record<TKey>
        {
            var result = FindWhere<TRecord>(criteria, instance);
            return result ?? instance;
        }
    }
}
