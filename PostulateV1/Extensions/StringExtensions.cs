using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Extensions
{
    public static class StringExtensions
    {
        public static Dictionary<string, string> ParseTokens(this string input)
        {
            return input.Split(';')
                .Where(s =>
                {
                    string[] parts = s.Split('=');
                    return (parts.Length == 2);
                })
                .Select(s =>
                {
                    string[] parts = s.Split('=');
                    return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
                }).ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
