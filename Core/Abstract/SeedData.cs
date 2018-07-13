using Dapper;
using System;
using System.Collections.Generic;
using System.Data;

namespace Postulate.Orm.Abstract
{
	public abstract class SeedData<TRecord, TKey> where TRecord : Record<TKey>, new()
	{
		/// <summary>
		/// FROM and WHERE clause (without the word "FROM") used to determine whether a seed row exists or not.
		/// WHERE clause should include parameters
		/// </summary>
		public abstract string ExistsCriteria { get; }

		/// <summary>
		/// Array of records to generate
		/// </summary>
		public abstract IEnumerable<TRecord> Records { get; }

		/// <summary>
		/// Expression used with the <see cref="FindId{TLookup}(string)"/> method. Must use parameter called @name, but the column name is up to you
		/// </summary>
		protected virtual string FindIdExpression { get { return "[Name]=@name"; } }

		private IDbConnection _connection;
		private SqlDb<TKey> _db;

		public void Generate(IDbConnection connection, SqlDb<TKey> db, Action<TRecord> setProperties = null)
		{
			_connection = connection;
			_db = db;

			foreach (var record in Records)
			{
				// apply any tenant-specific properties, such as an OrgId
				setProperties?.Invoke(record);

				var existingRecord = connection.QuerySingleOrDefault<TRecord>($"SELECT * FROM {ExistsCriteria}", record);

				// this will cause the existing seed record to be updated instead of inserted
				if (existingRecord != null) record.Id = existingRecord.Id;

				db.Save(connection, record);
			}
		}

		/// <summary>
		/// Use in your <see cref="Records"/> property to reference generated identity values not known until runtime
		/// </summary>
		protected TKey FindId<TLookup>(string name) where TLookup : Record<TKey>, new()
		{
			try
			{
				return _db.FindWhere<TLookup>(_connection, FindIdExpression, new { name = name }).Id;
			}
			catch
			{
				throw new Exception($"Couldn't find {typeof(TLookup).Name} where {FindIdExpression} with name = {name}");
			}
		}

		public void Generate(SqlDb<TKey> db)
		{
			using (var cn = db.GetConnection())
			{
				cn.Open();
				Generate(cn, db);
			}
		}
	}
}