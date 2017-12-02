using Dapper;
using Postulate.Orm.Enums;
using Postulate.Orm.Exceptions;
using Postulate.Orm.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : ISqlDb
    {
        /// <summary>
        /// Inserts or updates the given records from an open connection. Does not set the record Id property unless the batchSize argument is 1 or less.
        /// </summary>
        /// <param name="batchSize">Number of records to process at a time. A value of 1 causes your Record overrides to execute, and the Record.Id is set. A value greater than one causes your overrides to be skipped.</param>
        public async Task SaveMultipleAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>, new()
        {
            await SaveMultipleInnerAsync(connection, records, batchSize, cancellationToken, progress);
        }

        /// <summary>
        /// Inserts or updates the given records. Does not set the record Id property unless the batchSize argument to 1 or less.
        /// </summary>
        /// <param name="batchSize">Number of records to process at a time. A value of 1 causes your Record overrides to execute, and the Record.Id is set. A value greater than one causes your overrides to be skipped.</param>
        public async Task SaveMultipleAsync<TRecord>(IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>, new()
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                await SaveMultipleInnerAsync(cn, records, batchSize, cancellationToken, progress);
            }
        }

        public void SaveMultiple<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize = 100) where TRecord : Record<TKey>, new()
        {
            if (batchSize > 1)
            {
                SaveInBatches(connection, records, batchSize);
            }
            else
            {
                foreach (var record in records) Save(connection, record);
            }
        }

        public void SaveMultiple<TRecord>(IEnumerable<TRecord> records, int batchSize = 100) where TRecord : Record<TKey>, new()
        {
            using (var cn = GetConnection())
            {
                cn.Open();
                SaveMultiple(cn, records, batchSize);
            }
        }

        private async Task SaveMultipleInnerAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>, new()
        {
            if (batchSize > 1)
            {
                await SaveInBatchesAsync(connection, records, batchSize, progress, cancellationToken);
            }
            else
            {
                await SaveEachInnerAsync(connection, records, progress, cancellationToken);
            }
        }

        private async Task SaveEachInnerAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, IProgress<int> progress, CancellationToken cancellationToken) where TRecord : Record<TKey>, new()
        {
            int percentDone = 0;
            int count = 0;
            int totalCount = records.Count();
            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await SaveAsync(connection, record);

                count++;
                percentDone = Convert.ToInt32(Convert.ToDouble(count) / Convert.ToDouble(totalCount) * 100);
                progress?.Report(percentDone);
            }
        }

        private async Task SaveInBatchesAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize, IProgress<int> progress, CancellationToken cancellationToken) where TRecord : Record<TKey>
        {
            Func<TRecord, bool> insertPredicate = (r) => { return r.IsNew(); };
            Func<TRecord, bool> updatePredicate = (r) => { return !r.IsNew(); };

            var operations = new[]
            {
                new { Action = SaveAction.Insert, Predicate = insertPredicate, Command = GetInsertStatement<TRecord>() },
                new { Action = SaveAction.Update, Predicate = updatePredicate, Command = GetUpdateStatement<TRecord>() }
            };

            // thanks to accepted answer at http://stackoverflow.com/questions/10689779/bulk-inserts-taking-longer-than-expected-using-dapper
            int batch = 0;
            do
            {
                if (cancellationToken.IsCancellationRequested) break;

                using (IDbTransaction trans = GetTransaction(connection))
                {
                    var subset = records.Skip(batch * batchSize).Take(batchSize);
                    if (!subset.Any()) break;

                    foreach (var op in operations)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var subsetRecords = subset.Where(r => op.Predicate.Invoke(r));

                        string errorMessage = null;
                        var invalidRecord = subset.FirstOrDefault(item => !item.IsValid(connection, op.Action, out errorMessage));
                        if (invalidRecord != null) throw new SaveException(errorMessage, op.Command, invalidRecord);

                        await connection.ExecuteAsync(op.Command, subsetRecords, trans);
                    }

                    trans.Commit();
                }
                batch++;
                progress?.Report(batch * batchSize);
            } while (true);
        }

        private void SaveInBatches<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize) where TRecord : Record<TKey>
        {
            Func<TRecord, bool> insertPredicate = (r) => { return r.IsNew(); };
            Func<TRecord, bool> updatePredicate = (r) => { return !r.IsNew(); };

            var operations = new[]
            {
                new { Action = SaveAction.Insert, Predicate = insertPredicate, Command = GetInsertStatement<TRecord>() },
                new { Action = SaveAction.Update, Predicate = updatePredicate, Command = GetUpdateStatement<TRecord>() }
            };

            // thanks to accepted answer at http://stackoverflow.com/questions/10689779/bulk-inserts-taking-longer-than-expected-using-dapper
            int batch = 0;
            do
            {
                using (IDbTransaction trans = GetTransaction(connection))
                {
                    var subset = records.Skip(batch * batchSize).Take(batchSize);
                    if (!subset.Any()) break;

                    foreach (var op in operations)
                    {
                        var subsetRecords = subset.Where(r => op.Predicate.Invoke(r));

                        string errorMessage = null;
                        var invalidRecord = subset.FirstOrDefault(item => !item.IsValid(connection, op.Action, out errorMessage));
                        if (invalidRecord != null) throw new SaveException(errorMessage, op.Command, invalidRecord);

                        connection.Execute(op.Command, subsetRecords, trans);
                    }

                    trans.Commit();
                }
                batch++;
            } while (true);
        }
    }
}