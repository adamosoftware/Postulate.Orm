﻿using Postulate.Orm.Attributes;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using Postulate.Orm.Models;
using ReflectionHelper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        public IEnumerable<PropertyChange> GetChanges<TRecord>(IDbConnection connection, TRecord record, string ignoreProps = null) where TRecord : Record<TKey>
        {
            if (record.IsNew()) return null;

            string[] ignorePropsArray = (ignoreProps ?? string.Empty).Split(',', ';').Select(s => s.Trim()).ToArray();

            TRecord savedRecord = Find<TRecord>(connection, record.Id);
            return typeof(TRecord).GetProperties().Where(pi => pi.HasColumnAccess(Access.UpdateOnly) && !ignorePropsArray.Contains(pi.Name)).Select(pi =>
            {
                return new PropertyChange()
                {
                    PropertyName = pi.Name,
                    OldValue = OnGetChangesPropertyValue(pi, savedRecord, connection),
                    NewValue = OnGetChangesPropertyValue(pi, record, connection)
                };
            }).Where(vc => vc.IsChanged());
        }

        protected virtual object OnGetChangesPropertyValue(PropertyInfo propertyInfo, object record, IDbConnection connection)
        {
            return propertyInfo.GetValue(record);
        }

        public void CaptureChanges<TRecord>(IDbConnection connection, TRecord record, string ignoreProps = null) where TRecord : Record<TKey>
        {
            var changes = GetChanges(connection, record, ignoreProps);
            if (changes?.Any() ?? false) OnCaptureChanges<TRecord>(connection, record.Id, changes);
        }

        protected abstract void OnCaptureChanges<TRecord>(IDbConnection connection, TKey id, IEnumerable<PropertyChange> changes) where TRecord : Record<TKey>;

        public abstract IEnumerable<ChangeHistory<TKey>> QueryChangeHistory<TRecord>(IDbConnection connection, TKey id, int timeZoneOffset = 0) where TRecord : Record<TKey>;

        private bool HasChangeTracking<TRecord>(out string ignoreProperties) where TRecord : Record<TKey>
        {
            TrackChangesAttribute attr;
            if (typeof(TRecord).HasAttribute(out attr))
            {
                ignoreProperties = attr.IgnoreProperties;
                return true;
            }
            ignoreProperties = null;
            return false;
        }
    }
}