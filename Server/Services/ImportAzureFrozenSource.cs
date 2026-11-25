using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GIBS.Module.FBClients.ImportData
{
    internal static class ImportAzureFrozenSource
    {
        internal static async Task<ImportBatchCheckpoint.SourceSnapshot> ReadAsync(SqlConnection source, string expectedDatabase,
            IReadOnlyList<(string Table, string Key)> tables, CancellationToken cancellationToken)
        {
            var (_, combined) = await ReadPairAsync(source, expectedDatabase, Array.Empty<(string Table, string Key)>(), tables, cancellationToken);
            return combined;
        }

        internal static async Task<(ImportBatchCheckpoint.SourceSnapshot Original, ImportBatchCheckpoint.SourceSnapshot Combined)> ReadPairAsync(
            SqlConnection source, string expectedDatabase, IReadOnlyList<(string Table, string Key)> originalTables,
            IReadOnlyList<(string Table, string Key)> combinedTables, CancellationToken cancellationToken)
        {
            await using var metadata = source.CreateCommand();
            metadata.CommandTimeout = 120;
            metadata.CommandText = "SELECT CONVERT(int,SERVERPROPERTY('EngineEdition')), CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')), DB_NAME(), d.database_id, d.create_date, d.state FROM sys.databases d WHERE d.database_id=DB_ID()";
            string server;
            string database;
            int databaseId;
            DateTime createdOn;
            await using (var reader = await metadata.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken) || reader.GetInt32(0) != 5 || reader.GetByte(5) != 0
                    || reader.IsDBNull(1)
                    || !string.Equals(reader.GetString(2), expectedDatabase, StringComparison.Ordinal))
                    throw new ValidationException("Azure rehearsal requires the exact host-approved frozen Azure SQL database copy and visible identity metadata.");
                server = reader.GetString(1);
                database = reader.GetString(2);
                databaseId = reader.GetInt32(3);
                createdOn = reader.GetDateTime(4);
                if (await reader.ReadAsync(cancellationToken))
                    throw new ValidationException("Azure source identity metadata is ambiguous; rehearsal is blocked.");
            }

            var (originalFingerprint, combinedFingerprint) = await FingerprintsAsync(source, originalTables, combinedTables, cancellationToken);
            return (new(server, database, databaseId, createdOn, 0, Guid.Empty, originalFingerprint),
                new(server, database, databaseId, createdOn, 0, Guid.Empty, combinedFingerprint));
        }

        private static async Task<(string Original, string Combined)> FingerprintsAsync(SqlConnection source,
            IReadOnlyList<(string Table, string Key)> originalTables, IReadOnlyList<(string Table, string Key)> combinedTables,
            CancellationToken cancellationToken)
        {
            var originalSet = originalTables.ToHashSet();
            var combinedSet = combinedTables.ToHashSet();
            if (originalSet.Count != originalTables.Count || combinedSet.Count != combinedTables.Count
                || !originalSet.IsSubsetOf(combinedSet)
                || combinedTables.Select(item => item.Table).Distinct(StringComparer.Ordinal).Count() != combinedTables.Count)
                throw new ValidationException("Frozen-source fingerprint table selection is ambiguous.");
            using var originalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var combinedHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var (table, key) in combinedTables.OrderBy(item => item.Table, StringComparer.Ordinal))
            {
                var hashes = originalSet.Contains((table, key)) ? new[] { originalHash, combinedHash } : new[] { combinedHash };
                await using var permissions = source.CreateCommand();
                permissions.CommandTimeout = 120;
                permissions.CommandText = "SELECT HAS_PERMS_BY_NAME(@table,'OBJECT','SELECT'), (SELECT COUNT_BIG(*) FROM sys.fn_my_permissions(@table,'OBJECT') WHERE permission_name IN ('INSERT','UPDATE','DELETE','ALTER','CONTROL','TAKE OWNERSHIP'))";
                permissions.Parameters.Add("@table", SqlDbType.NVarChar, 260).Value = "dbo." + table;
                await using (var reader = await permissions.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0) || reader.GetInt32(0) != 1)
                        throw new ValidationException($"The Azure source account requires SELECT on dbo.{table}.");
                    if (reader.GetInt64(1) != 0)
                        throw new ValidationException($"Use a dedicated read-only Azure source account without write or schema permissions on dbo.{table}.");
                }
                var tableBytes = Encoding.Unicode.GetBytes(table + "\0");
                foreach (var hash in hashes) hash.AppendData(tableBytes);
                await using var schema = source.CreateCommand();
                schema.CommandTimeout = 120;
                schema.CommandText = "SELECT c.column_id,c.name,c.system_type_id,c.user_type_id,c.max_length,c.precision,c.scale,c.is_nullable,c.collation_name,c.is_computed FROM sys.columns c WHERE c.object_id=OBJECT_ID(@table,'U') ORDER BY c.column_id FOR JSON PATH, INCLUDE_NULL_VALUES";
                schema.Parameters.Add("@table", SqlDbType.NVarChar, 260).Value = "dbo." + table;
                await AppendJsonAsync(schema, hashes, cancellationToken);
                await using var rows = source.CreateCommand();
                rows.CommandTimeout = 120;
                var quotedTable = "[" + table.Replace("]", "]]") + "]";
                var quotedKey = "[" + key.Replace("]", "]]") + "]";
                // Only merged-family IDs affect the active Christmas classification.
                rows.CommandText = table == "GIBS_FBClientsAddFamMem_Merged"
                    ? $"SELECT DISTINCT {quotedKey} FROM dbo.{quotedTable} ORDER BY {quotedKey} FOR JSON PATH, INCLUDE_NULL_VALUES"
                    : $"SELECT * FROM dbo.{quotedTable} ORDER BY {quotedKey} FOR JSON PATH, INCLUDE_NULL_VALUES";
                await AppendJsonAsync(rows, hashes, cancellationToken);
            }
            return (Convert.ToHexString(originalHash.GetHashAndReset()), Convert.ToHexString(combinedHash.GetHashAndReset()));
        }

        private static async Task AppendJsonAsync(SqlCommand command, IReadOnlyList<IncrementalHash> hashes, CancellationToken cancellationToken)
        {
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            var buffer = new char[4096];
            while (await reader.ReadAsync(cancellationToken))
            {
                using var text = reader.GetTextReader(0);
                int count;
                while ((count = await text.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                {
                    var bytes = Encoding.Unicode.GetBytes(buffer, 0, count);
                    foreach (var hash in hashes) hash.AppendData(bytes);
                }
            }
            var separator = Encoding.Unicode.GetBytes("\0");
            foreach (var hash in hashes) hash.AppendData(separator);
        }
    }
}
