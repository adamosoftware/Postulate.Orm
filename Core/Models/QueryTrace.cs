using Postulate.Orm.Abstract;
using System.Collections.Generic;
using System.Linq;
using Postulate.Orm.Enums;
using System.Data;
using Postulate.Orm.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Postulate.Orm.Models
{
    [Schema("log")]
    public class QueryTrace : Record<int>
    {
        //TKey is hard-coded to int so that Query<> won't require TKey argument

        public QueryTrace()
        {
        }

        public QueryTrace(string queryClass, string userName, string sql, IEnumerable<Parameter> parameters, long duration, string context)
        {
            QueryClass = queryClass;
            UserName = userName;
            Sql = sql;
            Parameters = parameters;
            Duration = duration;
            Context = context;
        }

        [MaxLength(100)]
        public string QueryClass { get; set; }

        [MaxLength(100)]
        public string UserName { get; set; }
        
        public string Sql { get; set; }
        public string ParameterValues { get; set; }
        public IEnumerable<Parameter> Parameters { get; set; }
        public long Duration { get; set; }

        [MaxLength(100)]
        public string Context { get; set; }

        public string GetParameterValueString()
        {
            return string.Join(", ", Parameters.Select(pi => $"{pi.Name} = {pi.Value}"));
        }

        public override void BeforeSave(IDbConnection connection, SqlDb<int> db, SaveAction action)
        {
            if (Parameters != null) ParameterValues = GetParameterValueString();
        }

        public class Parameter
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}