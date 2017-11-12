using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using Postulate.Orm.Enums;
using System.Data;

namespace Testing.Models
{
    [ForeignKey("OrganizationId", typeof(Organization))]
    [ColumnAccess("OrganizationId", Access.InsertOnly)]
    public abstract class BaseTable : Record<int>
    {
        [ColumnAccess(Access.InsertOnly)]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [ColumnAccess(Access.InsertOnly)]
        [MaxLength(20)]
        [Required]
        public string CreatedBy { get; set; }

        [ColumnAccess(Access.UpdateOnly)]
        public DateTime? DateModified { get; set; }

        [ColumnAccess(Access.UpdateOnly)]
        [MaxLength(20)]
        public string ModifiedBy { get; set; }        

        public override void BeforeSave(IDbConnection connection, SqlDb<int> db, string userName, SaveAction action)
        {
            switch (action)
            {
                case SaveAction.Insert:
                    CreatedBy = userName;
                    DateCreated = DateTime.Now;
                    break;

                case SaveAction.Update:
                    ModifiedBy = userName;
                    DateModified = DateTime.Now;
                    break;
            }
        }
    }
}
