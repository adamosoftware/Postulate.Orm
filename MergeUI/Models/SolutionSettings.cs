using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.MergeUI.Models
{
    public class SolutionSettings
    {
        /// <summary>
        /// Assembly in the solution that contains model classes that will be merged to the database
        /// </summary>
        public string ModelAssembly { get; set; }
    }
}
