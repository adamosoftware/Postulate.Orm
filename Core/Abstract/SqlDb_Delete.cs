using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using System.Data;

namespace Postulate.Orm.Abstract
{
	public abstract partial class SqlDb<TKey> : ISqlDb
	{
		protected abstract void OnCaptureDeletion<TRecord>(IDbConnection connection, TRecord record, IDbTransaction transasction) where TRecord : Record<TKey>;

		protected abstract TRecord BeginRestore<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>;

		protected abstract void CompleteRestore<TRecord>(IDbConnection connection, TKey id, IDbTransaction transaction) where TRecord : Record<TKey>;

		public void DeleteOne<TRecord>(TRecord record) where TRecord : Record<TKey>
		{
			using (IDbConnection cn = GetConnection())
			{
				cn.Open();
				DeleteOne(cn, record);
			}
		}

		public void DeleteOne<TRecord>(TKey id) where TRecord : Record<TKey>, new()
		{
			using (IDbConnection cn = GetConnection())
			{
				cn.Open();
				DeleteOne<TRecord>(cn, id);
			}
		}

		public void DeleteOne<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>, new()
		{
			TRecord record = Find<TRecord>(connection, id);
			if (record != null) DeleteOne(connection, record);
		}

		public void DeleteOne<TRecord>(IDbConnection connection, TRecord record) where TRecord : Record<TKey>
		{
			string message;
			if (record.AllowDelete(connection, this, out message))
			{
				record.BeforeDelete(connection, this);

				if (TrackDeletions<TRecord>())
				{
					using (var txn = GetTransaction(connection))
					{
						try
						{
							OnCaptureDeletion(connection, record, txn);
							ExecuteDeleteOne<TRecord>(connection, record.Id, txn);
							txn.Commit();
						}
						catch
						{
							txn.Rollback();
							throw;
						}
					}
				}
				else
				{
					ExecuteDeleteOne<TRecord>(connection, record.Id);
				}

				record.AfterDelete(connection, this);
			}
			else
			{
				throw new PermissionDeniedException(message);
			}
		}

		public void DeleteOneWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>, new()
		{
			using (IDbConnection cn = GetConnection())
			{
				cn.Open();
				DeleteOneWhere<TRecord>(cn, criteria, parameters);
			}
		}

		public void DeleteOneWhere<TRecord>(IDbConnection connection, string criteria, object parameters) where TRecord : Record<TKey>, new()
		{
			TRecord record = FindWhere<TRecord>(connection, criteria, parameters);
			if (record != null) DeleteOne<TRecord>(connection, record.Id);
		}

		public int DeleteAllWhere<TRecord>(string criteria, object parameters) where TRecord : Record<TKey>
		{
			using (IDbConnection cn = GetConnection())
			{
				cn.Open();
				return DeleteAllWhere<TRecord>(cn, criteria, parameters);
			}
		}

		public int DeleteAllWhere<TRecord>(IDbConnection connection, string criteria, object parameters) where TRecord : Record<TKey>
		{
			string cmd = $"DELETE {GetTableName<TRecord>()} WHERE {criteria}";

			if (TrackDeletions<TRecord>())
			{
				var records = connection.Query<TRecord>($"SELECT * FROM {GetTableName<TRecord>()} WHERE {criteria}", parameters);

				using (var txn = GetTransaction(connection))
				{
					try
					{
						foreach (var record in records) OnCaptureDeletion(connection, record, txn);
						int result = connection.Execute(cmd, parameters, txn);
						txn.Commit();
						return result;
					}
					catch
					{
						txn.Rollback();
						throw;
					}
				}
			}
			else
			{
				return connection.Execute(cmd, parameters);
			}
		}

		public TKey RestoreOne<TRecord>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
		{
			var record = BeginRestore<TRecord>(connection, id);

			using (var txn = GetTransaction(connection))
			{
				try
				{
					TKey newId = ExecuteInsert(connection, record, txn);
					CompleteRestore<TRecord>(connection, id, txn);
					txn.Commit();
					return newId;
				}
				catch
				{
					txn.Rollback();
					throw;
				}
			}
		}

		public TKey RestoreOne<TRecord>(TKey id) where TRecord : Record<TKey>
		{
			using (var cn = GetConnection())
			{
				cn.Open();
				return RestoreOne<TRecord>(cn, id);
			}
		}

		private void ExecuteDeleteOne<TRecord>(IDbConnection connection, TKey id, IDbTransaction txn = null) where TRecord : Record<TKey>
		{
			string cmd = GetCommand<TRecord>(_deleteCommands, () => GetDeleteStatement<TRecord>());
			connection.Execute(cmd, new { id = id }, txn);
		}

		private bool TrackDeletions<TRecord>() where TRecord : Record<TKey>
		{
			return typeof(TRecord).HasAttribute<TrackDeletionsAttribute>();
		}
	}
}