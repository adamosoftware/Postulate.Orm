using Postulate.Orm.Abstract;
using Postulate.Orm.ModelMerge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Data;
using System.Linq;
using Postulate.Orm.SchemaMerge.Actions;
using Postulate.Orm.Models;

namespace Postulate.Orm.SchemaMerge
{
	/// <summary>
	/// Enables merging database objects from one connection to another
	/// </summary>
	/// <typeparam name="TSyntax">SQL dialect</typeparam>
	public class Engine<TSyntax> where TSyntax : SqlSyntax, new()
	{
		private readonly string _fromConnection;
		private readonly string _toConnection;
		protected readonly IProgress<MergeProgress> _progress;
		protected readonly TSyntax _syntax;

		private Stopwatch _stopwatch = null;

		public Engine(IProgress<MergeProgress> progress = null)
		{
			_progress = progress;
			_syntax = new TSyntax();
		}

		public Engine(string fromConnection, string toConnection, IProgress<MergeProgress> progress = null) : this(progress)
		{
			_fromConnection = fromConnection;
			_toConnection = toConnection;
		}

		public TSyntax Syntax { get { return _syntax; } }

		public Stopwatch Stopwatch { get { return _stopwatch; } }

		public async Task<IEnumerable<MergeAction>> CompareAsync()
		{			
			using (var fromConnection = _syntax.GetConnection(_fromConnection))
			{
				using (var toConnection = _syntax.GetConnection(_toConnection))
				{
					return await CompareAsync(fromConnection, toConnection);
				}
			}			
		}

		public async Task<IEnumerable<MergeAction>> CompareAsync(IDbConnection fromConnection, IDbConnection toConnection)
		{
			List<MergeAction> results = new List<MergeAction>();

			_stopwatch = Stopwatch.StartNew();

			await Task.Run(() =>
			{
				if (_syntax.SupportsSchemas)
				{
					// schemas
				}

				results.AddRange(CreateTables(fromConnection, toConnection));

				var newTables = results.OfType<CreateTable>().Select(a => a.TableInfo);
				results.AddRange(CreateColumns(fromConnection, toConnection, newTables));

				// new foreign keys

				// new indexes

				// altered columns (nullability, type or default changed)

				// altered keys (altered means we drop and create in the same action)

				// altered foreign keys

				// altered indexes

				// dropped columns

				// dropped tables

				// dropped indexes

				// dropped foreign keys

			});

			_stopwatch.Stop();

			return results;
		}

		private IEnumerable<MergeAction> CreateColumns(IDbConnection fromConnection, IDbConnection toConnection, IEnumerable<TableInfo> newTables)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<MergeAction> CreateTables(IDbConnection fromConnection, IDbConnection toConnection)
		{
			_progress?.Report(new MergeProgress() { Description = "Finding new tables..." });

			var fromTables = _syntax.GetTables(fromConnection);
			var toTables = _syntax.GetTables(toConnection);
			return fromTables.Where(tbl => !toTables.Contains(tbl)).Select(tbl => new CreateTable(_syntax, tbl));
		}
	}
}