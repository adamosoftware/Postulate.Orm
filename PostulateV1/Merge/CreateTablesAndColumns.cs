using Postulate.Abstract;
using Postulate.Attributes;
using Postulate.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Merge
{
    public partial class SchemaMerge<TDb, TKey> where TDb : SqlDb<TKey>, new()
    {
        /// <summary>
        /// Creates tables and columns that exist in the model but not in the schema
        /// </summary>
        private IEnumerable<Diff> CreateTablesAndColumns(IDbConnection connection)
        {
            /* I do tables and columns together because I need to know what tables are created so I don't mistake
               the columns in those tables as standalone additions. Previously, I used an instance variable
               to record created tables, but I would like to avoid that going forward */

            List<Diff> results = new List<Diff>();

            var schemaTables = GetSchemaTables(connection);
            var addTables = _modelTypes.Where(t => !schemaTables.Any(st => st.Equals(t)));
            results.AddRange(addTables.Select(t => new CreateTable(t)));

            var schemaColumns = GetSchemaColumns(connection);            
            var addColumns = _modelTypes.Where(t => !results.OfType<CreateTable>().Any(ct => ct.Equals(t)))
                .SelectMany(t => t.GetProperties()
                    .Where(pi =>
                        pi.CanWrite &&
                        IsSupportedType(pi.PropertyType) &&
                        !schemaColumns.Any(cr => cr.Equals(pi)) &&
                        !pi.Name.ToLower().Equals(nameof(Record<int>.Id).ToLower()) &&
                        !pi.HasAttribute<NotMappedAttribute>()));

            results.AddRange(addColumns.Select(pi => new AddColumn(pi)));

            return results;
        }        
    }    
}
