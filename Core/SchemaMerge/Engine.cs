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

				var fromTables = _syntax.GetTables(fromConnection);
				var toTables = _syntax.GetTables(toConnection);

				results.AddRange(CreateTables(fromTables, toTables));

				var fromColumns = _syntax.GetColumns(fromConnection);
				var toColumns = _syntax.GetColumns(toConnection);

				results.AddRange(CreateColumns(fromTables, fromConnection, toTables, toConnection, results));

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

		private IEnumerable<MergeAction> CreateColumns(
			IEnumerable<TableInfo> fromTables, IDbConnection fromConnection,
			IEnumerable<TableInfo> toTables,  IDbConnection toConnection,
			IEnumerable<MergeAction> existingActions)
		{
			_progress?.Report(new MergeProgress() { Description = "Finding new columns..." });

			var excludeNewTables = existingActions.OfType<CreateTable>().Select(a => a.TableInfo);

			

			throw new NotImplementedException();
		}

		private IEnumerable<MergeAction> CreateTables(IEnumerable<TableInfo> fromTables, IEnumerable<TableInfo> toTables)
		{
			_progress?.Report(new MergeProgress() { Description = "Finding new tables..." });

			return fromTables.Where(tbl => !toTables.Contains(tbl)).Select(tbl => new CreateTable(_syntax, tbl));
		}
	}
}