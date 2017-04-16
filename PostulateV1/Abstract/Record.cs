using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Abstract
{
    public abstract class Record<TKey>
    {
        internal static string IdColumnName {  get { return nameof(Id); } }

        private TKey _id;
        public TKey Id
        {
            get { return _id; }
            set { if (IsNewRow()) { _id = value; } else { throw new InvalidOperationException("Can't set the Id property more than once."); } }
        }

        /// <summary>
        /// Returns true if the record has never been saved before
        /// </summary>        
        public bool IsNewRow()
        {
            return (Id.Equals(default(TKey)));
        }

        public virtual bool IsValid(IDbConnection connection, out string message)
        {
            // todo: check required fields, etc
            message = null;
            return true;
        }

        /// <summary>
        /// Override this to determine if a given user has permission to view this record
        /// </summary>
        public virtual bool AllowFind(IDbConnection connection, string userName, out string message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Override this to perform actions after a record is saved
        /// </summary>
        public virtual void AfterSave(IDbConnection connection, SaveAction action)
        {
            // do nothing by default
        }

        /// <summary>
        /// Override this to determine whether a given user is allowed to save this record
        /// </summary>
        public virtual bool AllowSave(IDbConnection connection, string userName, out string message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Use this to set any properties that should update every time a record is saved, for example DateModified or ModifiedBy
        /// </summary>
        public virtual void BeforeSave(IDbConnection connection, SaveAction action)
        {
            // do nothing by default
        }

        public virtual bool AllowDelete(IDbConnection connection, string userName, out string message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// Override this to perform an action after a record is successfully deleted
        /// </summary>
        public virtual void AfterDelete(IDbConnection connection)
        {
            // do nothing by default
        }
    }
}
