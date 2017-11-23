using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Postulate.Orm.Extensions
{
    internal static class InternalStringExtensions
    {
        internal const string OrderByToken = "{orderBy}";
        internal const string WhereToken = "{where}";
        internal const string AndWhereToken = "{andWhere}";
        internal const string SqlParamRegex = "@([a-zA-Z][a-zA-Z0-9_]*)";

        /// <summary>
        /// Returns the defined parameter names within a SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to analyze</param>
        /// <param name="cleaned">Set true to omit the leading @ sign</param>
        /// <returns></returns>
        internal static IEnumerable<string> GetParameterNames(this string sql, bool cleaned = false)
        {
            var matches = Regex.Matches(sql, SqlParamRegex);
            return matches.OfType<Match>().Select(m => (cleaned) ? m.Value.Substring(1) : m.Value);
        }

        internal static bool ContainsAny(this string input, IEnumerable<string> substrings, out string substring)
        {
            substring = null;

            foreach (string item in substrings)
            {
                if (input.Contains(item))
                {
                    substring = item;
                    return true;
                }
            }

            return false;
        }

        internal static string ClearTokens(this string sql)
        {
            string result = sql;
            foreach (var token in new string[] { WhereToken, AndWhereToken, OrderByToken }) result = result.Replace(token, string.Empty);
            return result;
        }
    }
}