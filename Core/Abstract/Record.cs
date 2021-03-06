﻿using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

		public SaveAction GetSaveAction()
		{
			return (IsNew()) ? SaveAction.Insert : SaveAction.Update;
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

		public bool IsValid(IDbConnection connection, out string message)
		{
			return IsValid(connection, GetSaveAction(), out message);
		}

		/// <summary>
		/// Returns any validation errors on the record
		/// </summary>
		public virtual IEnumerable<string> GetValidationErrors(IDbConnection connection, SaveAction action)
		{
			foreach (var prop in GetType().GetProperties().Where(pi => pi.IsForSaveAction(action)))
			{
				if (prop.HasAttribute<PrimaryKeyAttribute>())
				{
					object value = prop.GetValue(this);
					if (value == null) yield return $"Primary key field {prop.Name} requires a value.";
				}

				if (!prop.HasAttribute<NotMappedAttribute>() && RequiredDateNotSet(prop))
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
		/// Override this to set any properties of a record, and execute foreign key lookups before it's viewed via the Find or FindWhere methods
		/// </summary>
		public virtual void BeforeView(IDbConnection connection, SqlDb<TKey> db)
		{
			// do nothing by default
		}

		/// <summary>
		/// Override this to determine if a given user has permission to view this record
		/// </summary>
		public virtual bool AllowView(IDbConnection connection, SqlDb<TKey> db, out string message)
		{
			message = null;
			return true;
		}

		/// <summary>
		/// Override this to perform actions after a record is saved
		/// </summary>
		public virtual void AfterSave(IDbConnection connection, SqlDb<TKey> db, SaveAction action)
		{
			// do nothing by default
		}

		/// <summary>
		/// Override this to determine whether a given user is allowed to save this record
		/// </summary>
		public virtual bool AllowSave(IDbConnection connection, SqlDb<TKey> db, out string message)
		{
			message = null;
			return true;
		}

		/// <summary>
		/// Use this to set any properties that should update every time a record is saved, for example user and datestamps
		/// </summary>
		public virtual void BeforeSave(IDbConnection connection, SqlDb<TKey> db, SaveAction action)
		{
			// do nothing by default
		}

		public virtual bool AllowDelete(IDbConnection connection, SqlDb<TKey> db, out string message)
		{
			message = null;
			return true;
		}

		/// <summary>
		/// Override this to perform an action before a record is deleted
		/// </summary>
		public virtual void BeforeDelete(IDbConnection connection, SqlDb<TKey> db)
		{
			// do nothing by default
		}

		/// <summary>
		/// Override this to perform an action after a record is successfully deleted
		/// </summary>
		public virtual void AfterDelete(IDbConnection connection, SqlDb<TKey> db)
		{
			// do nothing by default
		}

		/// <summary>
		/// Override this to set your own query for use with Find and FindWhere methods. Don't include any criteria, and make sure
		/// there is exactly one column named "Id" in the column list
		/// </summary>
		public virtual string CustomFindCommandText()
		{
			return null;
		}

		/// <summary>
		/// Override this to set your own Where clause used with the Find method. Don't include the word "WHERE", just use an expression only with single parameter named "@id"
		/// </summary>
		public virtual string CustomFindWhereClause()
		{
			return null;
		}

		/// <summary>
		/// Override this and parse the originalMessage to determine a more friendly error message
		/// </summary>
		/// <param name="originalMessage">Message from the original exception</param>
		/// <returns></returns>
		public virtual string GetErrorMessage(SqlDb<TKey> db, string originalMessage)
		{
			return originalMessage;
		}
	}
}