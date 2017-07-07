namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey>
    {
        public TRecord FindOrCreate<TRecord>(string criteria, TRecord instance) where TRecord : Record<TKey>
        {
            var result = FindWhere<TRecord>(criteria, instance);
            return result ?? instance;
        }
    }
}
