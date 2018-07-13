using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace Postulate.Orm.SqlServer
{
	public partial class SqlServerDb<TKey> : SqlDb<TKey>, ISqlDb
	{
		public override void CreateIfNotExists(Action<IDbConnection, bool> seedAction = null)
		{
			bool created = false;
			try
			{
				using (var cn = GetConnection())
				{
					cn.Open();
				}
			}
			catch
			{
				TryCreateDb(ConnectionString);
				created = true;
			}

			using (var cn = GetConnection())
			{
				cn.Open();
				seedAction?.Invoke(cn, created);
			}
		}

		private static IDbConnection TryGetMasterDb(string connectionString, out string dbName)
		{
			var tokens = connectionString.ParseTokens();
			var dbTokens = new string[] { "Database", "Initial Catalog" };
			dbName = Coalesce(tokens, dbTokens);
			string masterConnection = JoinReplace(tokens, dbTokens, "master");
			return new SqlConnection(masterConnection);
		}

		private static string JoinReplace(Dictionary<string, string> tokens, string[] lookForKey, string setValue)
		{
			string key = lookForKey.First(item => tokens.ContainsKey(item));
			tokens[key] = setValue;
			return string.Join(";", tokens.Select(keyPair => $"{keyPair.Key}={keyPair.Value}"));
		}

		internal static string ParseConnectionInfo(IDbConnection connection)
		{
			Dictionary<string, string> nameParts = connection.ConnectionString.ParseTokens();

			return $"{Coalesce(nameParts, "Data Source", "Server")}.{Coalesce(nameParts, "Database", "Initial Catalog")}";
		}

		private static string Coalesce(Dictionary<string, string> dictionary, params string[] keys)
		{
			string key = keys.First(item => dictionary.ContainsKey(item));
			return dictionary[key];
		}

		private void TryCreateDb(string connectionString)
		{
			string dbName;
			using (var cnMaster = TryGetMasterDb(connectionString, out dbName))
			{
				cnMaster.Open();
				cnMaster.Execute($"CREATE DATABASE [{dbName}]", commandTimeout: 60);

				int openAttempt = 0;
				const int maxAttempts = 100;
				while (openAttempt < 100)
				{
					try
					{
						openAttempt++;
						using (var cn = GetConnection())
						{
							cn.Open();
						}
					}
					catch
					{
						Thread.Sleep(150);
						if (openAttempt > maxAttempts) throw new Exception($"Couldn't open connection to {dbName}.");
					}
				}
			}
		}
	}
}