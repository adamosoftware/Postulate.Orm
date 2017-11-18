using System.Collections.Generic;
using System.Linq;

namespace Postulate.Orm.Models
{
    public class QueryTrace
    {
        public string UserName { get; private set; }
        public string Sql { get; private set; }
        public IEnumerable<Parameter> Parameters { get; private set; }
        public long Duration { get; private set; }
        public string Context { get; private set; }

        public string GetParameterValueString()
        {
            return string.Join(", ", Parameters.Select(pi => $"{pi.Name} = {pi.Value}"));
        }

        public QueryTrace(string userName, string sql, IEnumerable<Parameter> parameters, long duration, string context)
        {
            UserName = userName;
            Sql = sql;
            Parameters = parameters;
            Duration = duration;
            Context = context;
        }

        public class Parameter
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}