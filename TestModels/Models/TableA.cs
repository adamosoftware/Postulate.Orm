using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Testing.Models
{
    [TrackChanges(IgnoreProperties = "DateModified")]
    public class TableA : BaseTable
    {
        [PrimaryKey]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [PrimaryKey]
        [MaxLength(50)]
        public string LastName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime? EffectiveDate { get; set; }

        [DefaultExpression("1")]
        public long Value { get; set; }
    }

    public class TableASeedData : SeedData<TableA, int>
    {
        public override string ExistsCriteria => "[dbo].[TableA] WHERE [FirstName]=@firstName AND [LastName]=@lastName";

        public override IEnumerable<TableA> Records => new TableA[]
        {
            new TableA() { FirstName = "Whoever", LastName = "Whatever", Description = "yadda yadda" },
            new TableA() { FirstName = "Jimminy", LastName = "Hambone", EffectiveDate = new DateTime(1990, 1, 1) }
        };
    }
}
