using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Models
{
    [Schema("app")]
    public abstract class AppTable : Record<int>
    {
    }
}
