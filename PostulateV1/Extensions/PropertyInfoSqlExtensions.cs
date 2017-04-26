using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Extensions
{
    public static class PropertyInfoSqlExtensions
    {
        public static string SqlColumnName(this PropertyInfo propertyInfo)
        {
            string result = propertyInfo.Name;

            ColumnAttribute attr;
            if (propertyInfo.HasAttribute(out attr, a => !string.IsNullOrEmpty(a.Name))) result = attr.Name;

            return result;
        }
    }
}
