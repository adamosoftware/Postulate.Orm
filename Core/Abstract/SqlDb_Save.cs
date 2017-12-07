using Dapper;
using Postulate.Orm.Enums;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Interfaces;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Postulate.Orm.Abstract
{
	public abstract partial class SqlDb<TKey> : ISqlDb
	{
		public void Save<TRecord>(TRecord record, out SaveAction action) where TRecord : Record<TKey>, new()
		{
			using (IDbConnection cn = GetConnection())
			{
				cn.Open();
				Save(cn, record, out action);
			}
		}

		public void Save<TRecord>(TRecord record) where TRecord : Record<TKey>, new()
		{
			using (IDbConnection cn = GetConnection())
			{
				cn.Open();
				SaveAction action;
				Save(cn, record, out action);
			}
		}

		public void Save<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>, new()
		{
			SaveAction action;
			Save(connection, record, out action, transaction);
		}

		public void Save<TRecord>(IDbConnection connection, TRecord record, out SaveAction action, IDbTransaction transaction = null) where TRecord : Record<TKey>, new()
		{
			action = (record.IsNew()) ? SaveAction.Insert : SaveAction.Update;

			SaveInner(connection, record, action, (r, txn) =>
			{
				if (r.IsNew())
				{
					r.Id = ExecuteInsert(connection, r, txn);
				}
				else
				{
					ExecuteUpdate(connection, r, txn);
				}
			}, transaction);
		}

		private void SaveInner<TRecord>(IDbConnection connection, TRecord record, SaveAction action, Action<TRecord, IDbTransaction> saveAction, IDbTransaction transaction = null) where TRecord : Record<TKey>, new()
		{
			record.BeforeSave(connection, this, action);

			string message;
			if (record.IsValid(connection, action, out message))
			{
				if (record.AllowSave(connection, this, out message))
				{
					string ignoreProps;
					if (action == SaveAction.Update && TrackChanges<TRecord>(out ignoreProps)) CaptureChanges(connection, record, ignoreProps);

					saveAction.Invoke(record, transaction);

					record.AfterSave(connection, this, action);
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
			TKey result = default(TKey);

			string cmd = GetCommand<TRecord>(_insertCommands, () => GetInsertStatement<TRecord>());
			try
			{
				Stopwatch sw = Stopwatch.StartNew();
				result = ExecuteInsertMethod(connection, record, transaction, cmd);
				sw.Stop();
				InvokeTraceCallback(connection, "Save.Insert", cmd, record, sw);
			}
			catch (Exception exc)
			{
				throw new SaveException(exc.Message, cmd, record);
			}

			return result;
		}

		protected virtual TKey ExecuteInsertMethod<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction, string cmd) where TRecord : Record<TKey>
		{
			return connection.QuerySingle<TKey>(cmd, record, transaction);
		}

		private void ExecuteUpdate<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>
		{
			string cmd = GetCommand<TRecord>(_updateCommands, () => GetUpdateStatement<TRecord>());
			try
			{
				Stopwatch sw = Stopwatch.StartNew();
				ExecuteUpdateMethod(connection, record, transaction, cmd);
				sw.Stop();
				InvokeTraceCallback(connection, "Save.Update", cmd, record, sw);
			}
			catch (Exception exc)
			{
				throw new SaveException(exc.Message, cmd, record);
			}
		}

		protected virtual void ExecuteUpdateMethod<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction, string cmd) where TRecord : Record<TKey>
		{
			connection.Execute(cmd, record, transaction);
		}

		public async Task SaveAsync<TRecord>(TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>, new()
		{
			using (var cn = GetConnection())
			{
				cn.Open();
				await SaveAsync(cn, record, transaction);
			}
		}

		public async Task SaveAsync<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>, new()
		{
			var action = (record.IsNew()) ? SaveAction.Insert : SaveAction.Update;

			record.BeforeSave(connection, this, action);

			string message;
			if (record.IsValid(connection, action, out message))
			{
				if (record.AllowSave(connection, this, out message))
				{
					string ignoreProps;
					if (action == SaveAction.Update && TrackChanges<TRecord>(out ignoreProps)) CaptureChanges(connection, record, ignoreProps);

					if (record.IsNew())
					{
						record.Id = await ExecuteInsertAsync(connection, record, transaction);
					}
					else
					{
						await ExecuteUpdateAsync(connection, record, transaction);
					}

					record.AfterSave(connection, this, action);
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

		private async Task<TKey> ExecuteInsertAsync<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>
		{
			string cmd = GetCommand<TRecord>(_insertCommands, () => GetInsertStatement<TRecord>());
			try
			{
				return await connection.QuerySingleAsync<TKey>(cmd, record, transaction);
			}
			catch (Exception exc)
			{
				throw new SaveException(exc.Message, cmd, record);
			}
		}

		private async Task ExecuteUpdateAsync<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transaction = null) where TRecord : Record<TKey>
		{
			string cmd = GetCommand<TRecord>(_updateCommands, () => GetUpdateStatement<TRecord>());
			try
			{
				await connection.ExecuteAsync(cmd, record, transaction);
			}
			catch (Exception exc)
			{
				throw new SaveException(exc.Message, cmd, record);
			}
		}
	}
}