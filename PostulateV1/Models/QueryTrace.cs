using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Models
{
    public class QueryTrace
    {
        public string Sql { get; private set; }
        public object Parameters { get; private set; }
        public long Runtime { get; private set; }

        public string GetParameterValueString()
        {
            var props = Parameters?.GetType().GetProperties() ?? Enumerable.Empty<PropertyInfo>();
            return string.Join(", ", props.Select(pi => $"{pi.Name} = {pi.GetValue(Parameters)}"));
        }

        public QueryTrace(string sql, object parameters, long runtime)
        {
            Sql = sql;
            Parameters = parameters;
            Runtime = runtime;
        }
    }
}