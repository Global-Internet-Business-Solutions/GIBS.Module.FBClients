using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GIBS.Module.FBClients.ImportData
{
    internal static class ImportBatchCheckpoint
    {
        internal record BatchProgress(int TableIndex, int LastKey, long ImportedRows, bool Completed);
        internal record BatchResult(int LastKey, int ImportedRows, bool TableCompleted);
        internal record SourceSnapshot(string Server, string Database, int DatabaseId, DateTime CreatedOn, int SourceDatabaseId, Guid DatabaseGuid,
            [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string ContentFingerprint = null);

        internal static async Task<BatchResult> WriteChunkAsync(int lastKey, int batchSize, int maxBatches,
            Func<int, Task<BatchResult>> writeBatch, System.Threading.CancellationToken cancellationToken)
        {
            if (lastKey < 0 || batchSize is < 1 or > 1000 || maxBatches is < 1 or > 1000 || writeBatch == null)
                throw new ValidationException("The transactional chunk configuration is invalid.");
            var importedRows = 0;
            for (var index = 0; index < maxBatches; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = await writeBatch(lastKey);
                if (batch == null || batch.ImportedRows < 0 || batch.ImportedRows > batchSize
                    || (batch.ImportedRows == 0 && (!batch.TableCompleted || batch.LastKey != lastKey))
                    || (batch.ImportedRows > 0 && batch.LastKey <= lastKey))
                    throw new ValidationException("A logical batch returned invalid progress; no chunk changes may be committed.");
                lastKey = batch.LastKey;
                importedRows = checked(importedRows + batch.ImportedRows);
                cancellationToken.ThrowIfCancellationRequested();
                if (batch.TableCompleted) return new(lastKey, importedRows, true);
            }
            return new(lastKey, importedRows, false);
        }

        internal static async Task<SourceSnapshot> ReadSnapshotAsync(SqlConnection source)
        {
            await using var command = source.CreateCommand();
            command.CommandTimeout = 30;
            command.CommandText = "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')), DB_NAME(), d.database_id, d.create_date, d.source_database_id, r.database_guid FROM sys.databases d JOIN sys.database_recovery_status r ON r.database_id=d.database_id WHERE d.database_id=DB_ID() AND d.source_database_id IS NOT NULL AND d.state=0 AND d.is_read_only=1";
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync() || reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(4) || reader.IsDBNull(5))
                throw new ValidationException("Import requires a visible, online SQL Server database snapshot and its database identity metadata as the immutable source.");
            return new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetDateTime(3), reader.GetInt32(4), reader.GetGuid(5));
        }

        internal static string Identity(SourceSnapshot source, int importModuleId, int destinationModuleId, int sourceModuleId, IReadOnlyList<string> orderedTables, string planIdentity)
        {
            if (source == null || importModuleId <= 0 || destinationModuleId <= 0 || sourceModuleId <= 0
                || orderedTables == null || orderedTables.Count == 0 || orderedTables.Any(string.IsNullOrWhiteSpace)
                || orderedTables.Distinct(StringComparer.Ordinal).Count() != orderedTables.Count || string.IsNullOrWhiteSpace(planIdentity))
                throw new ValidationException("The immutable import identity or table selection is invalid.");
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { Version = 2, Source = source, ImportModuleId = importModuleId, DestinationModuleId = destinationModuleId, SourceModuleId = sourceModuleId, Tables = orderedTables, Plan = planIdentity });
            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        internal static async Task<BatchProgress> ExecuteAsync(SqlConnection source, SqlConnection target, int importModuleId, int destinationModuleId,
            int sourceModuleId, IReadOnlyList<string> orderedTables, string planIdentity, Func<SqlConnection, SqlTransaction, BatchProgress, Task<BatchResult>> writeBatch,
            Func<Task<SourceSnapshot>> readSourceIdentity = null, SourceSnapshot verifiedSourceIdentity = null,
            System.Threading.CancellationToken cancellationToken = default, string requiredCompletedIdentity = null, int requiredCompletedTables = 0,
            string supplementaryRunKey = null, string independentRunKey = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source == null || source.State != ConnectionState.Open || target == null || target.State != ConnectionState.Open || writeBatch == null || orderedTables == null
                || (verifiedSourceIdentity != null && readSourceIdentity == null)
                || (requiredCompletedIdentity == null && supplementaryRunKey != null)
                || (requiredCompletedIdentity != null && (string.IsNullOrWhiteSpace(supplementaryRunKey) || supplementaryRunKey == "1main"))
                || (independentRunKey != null && (requiredCompletedIdentity != null || supplementaryRunKey != null
                    || string.IsNullOrWhiteSpace(independentRunKey) || independentRunKey is "1main" or "2supplemental")))
                throw new ValidationException("The checkpoint transaction arguments are invalid.");
            var tables = orderedTables.ToArray();
            // A reused request fingerprint is always rechecked before this batch can commit.
            var sourceIdentity = verifiedSourceIdentity ?? (readSourceIdentity == null ? await ReadSnapshotAsync(source) : await readSourceIdentity());
            var identity = Identity(sourceIdentity, importModuleId, destinationModuleId, sourceModuleId, tables, planIdentity);
            var tableCount = tables.Length;
            var runKey = independentRunKey ?? (requiredCompletedIdentity == null ? "1main" : supplementaryRunKey);
            if (runKey.Length > 128)
                throw new ValidationException("The supplementary import run key is too long.");
            await using var transaction = (SqlTransaction)await target.BeginTransactionAsync(IsolationLevel.Serializable);
            if (requiredCompletedIdentity != null)
            {
                await using var prerequisite = target.CreateCommand();
                prerequisite.Transaction = transaction;
                prerequisite.CommandTimeout = 120;
                prerequisite.CommandText = "SELECT ImportModuleId, RunIdentity, TableIndex, LastKey, ImportedRows, Completed FROM dbo.GIBSFBClientsImportCheckpoint WITH (UPDLOCK,HOLDLOCK) WHERE DestinationModuleId=@destination AND RunKey=@runKey";
                prerequisite.Parameters.Add("@destination", SqlDbType.Int).Value = destinationModuleId;
                prerequisite.Parameters.Add("@runKey", SqlDbType.NVarChar, 128).Value = "1main";
                await using var prerequisiteReader = await prerequisite.ExecuteReaderAsync();
                if (!await prerequisiteReader.ReadAsync() || prerequisiteReader.GetInt32(0) != importModuleId
                    || !string.Equals(prerequisiteReader.GetString(1), requiredCompletedIdentity, StringComparison.Ordinal)
                    || prerequisiteReader.GetInt32(2) != requiredCompletedTables || prerequisiteReader.GetInt32(3) != 0
                    || prerequisiteReader.GetInt64(4) < 0 || !prerequisiteReader.GetBoolean(5))
                    throw new ValidationException("Finish the original rehearsal with the same source, saved plan, and administrator before starting a supplementary import. Do not delete or change its checkpoint.");
            }
            await using var read = target.CreateCommand();
            read.Transaction = transaction;
            read.CommandTimeout = 120;
            read.CommandText = "SELECT ImportModuleId, RunIdentity, TableIndex, LastKey, ImportedRows, Completed FROM dbo.GIBSFBClientsImportCheckpoint WITH (UPDLOCK,HOLDLOCK) WHERE DestinationModuleId=@destination AND RunKey=@runKey";
            read.Parameters.Add("@destination", SqlDbType.Int).Value = destinationModuleId;
            read.Parameters.Add("@runKey", SqlDbType.NVarChar, 128).Value = runKey;
            BatchProgress progress;
            var exists = false;
            await using (var reader = await read.ExecuteReaderAsync())
            {
                exists = await reader.ReadAsync();
                if (exists)
                {
                    if (reader.GetInt32(0) != importModuleId || !string.Equals(reader.GetString(1), identity, StringComparison.Ordinal))
                        throw new ValidationException("The destination already has a checkpoint for a different source identity, frozen-source contents, or import plan. Stop and review recovery; do not delete the checkpoint to bypass this check.");
                    progress = new(reader.GetInt32(2), reader.GetInt32(3), reader.GetInt64(4), reader.GetBoolean(5));
                }
                else progress = new(0, 0, 0, false);
            }
            if (progress.TableIndex < 0 || progress.TableIndex > tableCount || progress.LastKey < 0 || progress.ImportedRows < 0
                || progress.Completed != (progress.TableIndex == tableCount) || (progress.Completed && progress.LastKey != 0))
                throw new ValidationException("The durable import checkpoint is invalid; do not resume this import.");
            if (progress.Completed)
            {
                await transaction.CommitAsync();
                return progress;
            }
            var batch = await writeBatch(target, transaction, progress);
            if (readSourceIdentity != null && sourceIdentity != await readSourceIdentity())
                throw new ValidationException("The frozen Azure source identity, schema, or contents changed during this chunk. The entire chunk was rolled back; stop the rehearsal and review recovery.");
            cancellationToken.ThrowIfCancellationRequested();
            if (batch == null || batch.ImportedRows < 0 || (batch.ImportedRows == 0 && !batch.TableCompleted)
                || (batch.ImportedRows == 0 && batch.LastKey != progress.LastKey)
                || (batch.ImportedRows > 0 && batch.LastKey <= progress.LastKey))
                throw new ValidationException("The batch returned an invalid checkpoint; no batch changes were committed.");
            var nextTable = progress.TableIndex + (batch.TableCompleted ? 1 : 0);
            var next = new BatchProgress(nextTable, batch.TableCompleted ? 0 : batch.LastKey,
                checked(progress.ImportedRows + batch.ImportedRows), nextTable == tableCount);
            await using var save = target.CreateCommand();
            save.Transaction = transaction;
            save.CommandTimeout = 120;
            save.CommandText = exists
                ? "UPDATE dbo.GIBSFBClientsImportCheckpoint SET TableIndex=@tableIndex, LastKey=@lastKey, ImportedRows=@rows, Completed=@completed WHERE DestinationModuleId=@destination AND RunKey=@runKey"
                : "INSERT dbo.GIBSFBClientsImportCheckpoint (DestinationModuleId,RunKey,ImportModuleId,RunIdentity,TableIndex,LastKey,ImportedRows,Completed) VALUES (@destination,@runKey,@importModule,@identity,@tableIndex,@lastKey,@rows,@completed)";
            save.Parameters.Add("@destination", SqlDbType.Int).Value = destinationModuleId;
            save.Parameters.Add("@runKey", SqlDbType.NVarChar, 128).Value = runKey;
            save.Parameters.Add("@importModule", SqlDbType.Int).Value = importModuleId;
            save.Parameters.Add("@identity", SqlDbType.NVarChar, 64).Value = identity;
            save.Parameters.Add("@tableIndex", SqlDbType.Int).Value = next.TableIndex;
            save.Parameters.Add("@lastKey", SqlDbType.Int).Value = next.LastKey;
            save.Parameters.Add("@rows", SqlDbType.BigInt).Value = next.ImportedRows;
            save.Parameters.Add("@completed", SqlDbType.Bit).Value = next.Completed;
            if (await save.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("The batch checkpoint was not saved.");
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync();
            return next;
        }
    }
}
