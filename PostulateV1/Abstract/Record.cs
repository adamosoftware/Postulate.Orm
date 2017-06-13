using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Abstract
{
    public abstract class Record<TKey>
    {
        internal static string IdentityColumnName { get { return nameof(Id); } }

        private TKey _id;

        public TKey Id
        {
            get { return _id; }
            set { if (IsNew()) { _id = value; } else { throw new InvalidOperationException("Can't set the Id property more than once."); } }
        }

        /// <summary>
        /// Returns true if the record has never been saved before
        /// </summary>
        public bool IsNew()
        {
            return (Id.Equals(default(TKey)));
        }

        /// <summary>
        /// Returns true if the record passes all validation rules
        /// </summary>
        public bool IsValid(IDbConnection connection, SaveAction action, out string message)
        {
            var messages = GetValidationErrors(connection, action);
            if (!messages.Any())
            {
                message = null;
                return true;
            }
            else
            {
                message = string.Join("\r\n", messages);
                return false;
            }
        }

        /// <summary>
        /// Returns any validation errors on the record
        /// </summary>
        public virtual IEnumerable<string> GetValidationErrors(IDbConnection connection, SaveAction action)
        {
            foreach (var prop in GetType().GetProperties().Where(pi => pi.IsForSaveAction(action)))
            {
                if (RequiredDateNotSet(prop))
                {
                    yield return $"The {prop.Name} date field requires a value.";
                }

                var postulateAttr = prop.GetCustomAttributes<Validation.ValidationAttribute>();
                foreach (var attr in postulateAttr)
                {
                    object value = prop.GetValue(this);
                    if (!attr.IsValid(prop, value, connection)) yield return attr.ErrorMessage;
                }

                var validationAttr = prop.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>();
                foreach (var attr in validationAttr)
                {
                    object value = prop.GetValue(this);
                    if (!attr.IsValid(value)) yield return attr.FormatErrorMessage(prop.Name);
                }
            }
        }

        private bool RequiredDateNotSet(PropertyInfo prop)
        {
            if (prop.PropertyType.Equals(typeof(DateTime)))
            {
                DateTime value = (DateTime)prop.GetValue(this);
                if (value.Equals(DateTime.MinValue)) return true;
            }
            return false;
        }

        /// <summary>
        /// Override this to set any properties of a record before it's viewed via the Find or FindWhere methods
        /// </summary>        
        public virtual void BeforeView(IDbConnection connection)
        {
            // do nothing by default
        }

        /// <summary>
        /// Override this to determine if a given user has permission to view this record
        /// </summary>
        public virtual bool AllowView(IDbConnection connection, string userName, out string message)
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
        /// Use this to set any properties that should update every time a record is saved, for example user and datestamps
        /// </summary>
        public virtual void BeforeSave(IDbConnection connection, string userName, SaveAction action)
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