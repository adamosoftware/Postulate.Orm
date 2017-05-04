using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Postulate
{
    public static class Sql
    {
        public static string WhereClause(WhereClauseTerm[] terms, out DynamicParameters parameters)
        {
            var included = terms.Where(t => t.Condition);
            if (!included.Any())
            {
                parameters = null;
                return null;
            }

            var result = "WHERE " + string.Join(" AND ", terms
                .Where(t => t.Condition)
                .Select(t => t.Expression));

            parameters = new DynamicParameters();
            foreach (WhereClauseTerm term in terms.Where(t => t.Condition))
            {
                parameters.Add(term.GetParameterName(), term.Value);
            }

            return result;
        }
    }

    public class WhereClauseTerm
    {
        public bool Condition { get; set; }
        public string Expression { get; set; }
        public object Value { get; set; }

        public WhereClauseTerm(bool condition, string expression, object value)
        {
            Condition = condition;
            Expression = expression;
            Value = value;
        }

        public string GetParameterName()
        {
            // thanks to http://stackoverflow.com/questions/307929/regex-for-parsing-sql-parameters
            var matches = Regex.Matches(Expression, "@([a-zA-Z][a-zA-Z0-9_]*)");
            return matches.OfType<Match>().Select(m => m.Value).ToArray().First();
        }
    }
}
