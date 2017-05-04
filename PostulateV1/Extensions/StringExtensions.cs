using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Extensions
{
    internal static class StringExtensions
    {
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
    }
}
