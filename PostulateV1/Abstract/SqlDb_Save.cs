using Dapper;
using Postulate.Orm.Enums;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Interfaces;
using System;
using System.Data;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        public void Save<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>
        {
            SaveAction action;
            Save(connection, record, out action, transaction);
        }

        public void Save<TRecord>(IDbConnection connection, TRecord record, out SaveAction action, IDbTransaction transaction = null) where TRecord : Record<TKey>
        {
            action = (record.IsNew()) ? SaveAction.Insert : SaveAction.Update;

            SaveInner(connection, record, action, (r, txn) =>
            {
                if (r.IsNew())
                {
                    r.Id = ExecuteInsert(connection, r);
                }
                else
                {
                    ExecuteUpdate(connection, r);
                }
            }, transaction);
        }

        private void SaveInner<TRecord>(IDbConnection connection, TRecord record, SaveAction action, Action<TRecord, IDbTransaction> saveAction, IDbTransaction transaction = null) where TRecord : Record<TKey>
        {
            record.BeforeSave(connection, UserName, action);

            string message;
            if (record.IsValid(connection, action, out message))
            {
                if (record.AllowSave(connection, UserName, out message))
                {
                    string ignoreProps;
                    if (action == SaveAction.Update && TrackChanges<TRecord>(out ignoreProps)) CaptureChanges(connection, record, ignoreProps);

                    saveAction.Invoke(record, transaction);

                    record.AfterSave(connection, action);
                }
                else
                {
                    throw new PermissionDeniedException(message);
                }
            }
            else
            {
                throw new ValidationException(message);
            }
        }

        private TKey ExecuteInsert<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_insertCommands, () => GetInsertStatement<TRecord>());
            try
            {
                return connection.QuerySingle<TKey>(cmd, record, transaction);
            }
            catch (Exception exc)
            {
                throw new SaveException(exc.Message, cmd, record);
            }
        }

        private void ExecuteUpdate<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>
        {
            string cmd = GetCommand<TRecord>(_updateCommands, () => GetUpdateStatement<TRecord>());
            try
            {
                connection.Execute(cmd, record, transaction);
            }
            catch (Exception exc)
            {
                throw new SaveException(exc.Message, cmd, record);
            }
        }
    }
}