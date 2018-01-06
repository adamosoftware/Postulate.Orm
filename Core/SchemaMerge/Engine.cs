using Postulate.Orm.Abstract;
using Postulate.Orm.ModelMerge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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

		public Engine(string fromConnection, string toConnection, IProgress<MergeProgress> progress = null)
		{
			_fromConnection = fromConnection;
			_toConnection = toConnection;
			_progress = progress;
			_syntax = new TSyntax();
		}

		public TSyntax Syntax { get { return _syntax; } }

		public Stopwatch Stopwatch { get { return _stopwatch; } }

		public async Task<IEnumerable<MergeAction>> CompareAsync()
		{
			using (var fromConnection = _syntax.GetConnection(_fromConnection))
			{
				using (var toConnection = _syntax.GetConnection(_toConnection))
				{

				}
			}

			throw new NotImplementedException();
		}
	}
}