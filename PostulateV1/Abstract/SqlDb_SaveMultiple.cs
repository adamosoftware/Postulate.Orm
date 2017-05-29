using Postulate.Orm.Exceptions;
using Postulate.Orm.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Postulate.Orm.Enums;
using Dapper;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        /// <summary>
        /// Inserts or updates the given records from an open connection. Does not set the record Id property unless the batchSize argument is 1 or less.
        /// </summary>
        public async Task SaveMultipleAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>
        {
            var exc = await SaveMultipleInnerAsync(connection, records, batchSize, cancellationToken, progress);
            if (exc != null) throw new SaveException(exc.Message, exc.CommandText, exc.Record);
        }

        /// <summary>
        /// Inserts or updates the given records. Does not set the record Id property unless the batchSize argument to 1 or less.
        /// </summary>
        public async Task SaveMultipleAsync<TRecord>(IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>
        {
            SaveException exc = null;
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                exc = await SaveMultipleInnerAsync(cn, records, batchSize, cancellationToken, progress);
            }
            if (exc != null) throw new SaveException(exc.Message, exc.CommandText, exc.Record);
        }

        private async Task<SaveException> SaveMultipleInnerAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize = 100, CancellationToken cancellationToken = default(CancellationToken), IProgress<int> progress = null) where TRecord : Record<TKey>
        {
            if (batchSize > 1)
            {
                return await SaveInBatches(connection, records, batchSize, progress, cancellationToken);
            }
            else
            {
                return await SaveEachInnerAsync(connection, records, progress, cancellationToken);
            }
        }

        private async Task<SaveException> SaveEachInnerAsync<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, IProgress<int> progress, CancellationToken cancellationToken) where TRecord : Record<TKey>
        {
            SaveException exc = null;

            await Task.Run(() =>
            {
                int percentDone = 0;
                int count = 0;
                int totalCount = records.Count();
                foreach (var record in records)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        Save(connection, record);
                    }
                    catch (SaveException excInner)
                    {
                        exc = excInner;
                        break;
                    }

                    count++;
                    percentDone = Convert.ToInt32(Convert.ToDouble(count) / Convert.ToDouble(totalCount) * 100);
                    progress?.Report(percentDone);
                }
            });

            return exc;
        }

        private async Task<SaveException> SaveInBatches<TRecord>(IDbConnection connection, IEnumerable<TRecord> records, int batchSize, IProgress<int> progress, CancellationToken cancellationToken) where TRecord : Record<TKey>
        {
            Func<TRecord, bool> insertPredicate = (r) => { return r.IsNew(); };
            Func<TRecord, bool> updatePredicate = (r) => { return !r.IsNew(); };

            var operations = new[]
            {
                new { Action = SaveAction.Insert, Predicate = insertPredicate, Command = GetInsertStatement<TRecord>() },
                new { Action = SaveAction.Update, Predicate = updatePredicate, Command = GetUpdateStatement<TRecord>() }
            };

            SaveException exc = null;

            await Task.Run(() =>
            {
                // thanks to accepted answer at http://stackoverflow.com/questions/10689779/bulk-inserts-taking-longer-than-expected-using-dapper
                int batch = 0;
                do
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    using (IDbTransaction trans = connection.BeginTransaction())
                    {
                        var subset = records.Skip(batch * batchSize).Take(batchSize);
                        if (!subset.Any()) break;

                        foreach (var op in operations)
                        {
                            var subsetRecords = subset.Where(r => op.Predicate.Invoke(r));

                            string errorMessage = null;
                            var invalidRecord = subset.FirstOrDefault(item => !item.IsValid(connection, op.Action, out errorMessage));
                            if (invalidRecord != null)
                            {
                                exc = new SaveException(errorMessage, op.Command, invalidRecord);
                                break;
                            }

                            connection.Execute(op.Command, subsetRecords, trans);
                        }

                        trans.Commit();
                    }
                    batch++;
                    progress?.Report(batch * batchSize);
                } while (true);
            });

            return exc;
        }
    }
}
