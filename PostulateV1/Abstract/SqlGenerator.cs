using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Abstract
{
    public abstract class SqlGenerator<TRecord, TKey> where TRecord : Record<TKey>
    {
    }
}
