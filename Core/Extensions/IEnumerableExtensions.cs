using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Orm.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// I couldn't get the built-in IEnumerable.Contains method to work as expected, so I wrote my own
        /// </summary>
        public static bool ContainsThis<T>(this IEnumerable<T> list, T value)
        {
            foreach (var item in list)
            {
                if (item.Equals(value)) return true;
            }
            return false;
        }
    }
}
