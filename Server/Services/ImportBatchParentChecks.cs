using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GIBS.Module.FBClients.ImportData
{
    internal static class ImportBatchParentChecks
    {
        internal enum FamilyDisposition { NotChristmas, Linked, ClientLevel, ArchivedFamily, Conflict }
        internal record ParentReference(int ClientId, int? LegacyFamilyId, FamilyDisposition Disposition);

        internal static async Task ValidateAsync(SqlConnection target, SqlTransaction transaction, int destinationModuleId,
            int sourceModuleId, IReadOnlyCollection<ParentReference> references)
        {
            if (target == null || target.State != ConnectionState.Open || transaction == null || transaction.Connection != target
                || transaction.IsolationLevel != IsolationLevel.Serializable || destinationModuleId <= 0 || sourceModuleId <= 0 || references == null)
                throw new ValidationException("Parent validation requires an open destination and its serializable batch transaction.");
            if (references.Count == 0) return;
            if (references.Any(reference => reference == null || reference.ClientId <= 0
                || !Enum.IsDefined(reference.Disposition)
                || (reference.Disposition == FamilyDisposition.NotChristmas && reference.LegacyFamilyId != null)
                || (reference.Disposition == FamilyDisposition.Linked && reference.LegacyFamilyId is null or <= 0)))
                throw new ValidationException("The batch contains an invalid parent reference.");
            if (references.Any(reference => reference.Disposition == FamilyDisposition.Conflict))
                throw new ValidationException("A Christmas record references an active family member belonging to another client. The batch was not imported.");

            using var rows = new DataTable();
            rows.Columns.Add("ClientId", typeof(int));
            rows.Columns.Add("LegacyFamilyId", typeof(int));
            rows.Columns.Add("Disposition", typeof(int));
            foreach (var reference in references.Distinct())
                rows.Rows.Add(reference.ClientId, (object)reference.LegacyFamilyId ?? DBNull.Value, (int)reference.Disposition);

            await using var create = target.CreateCommand();
            create.Transaction = transaction;
            create.CommandTimeout = 120;
            create.CommandText = "CREATE TABLE #FBImportParents (ClientId int NOT NULL, LegacyFamilyId int NULL, Disposition int NOT NULL)";
            await create.ExecuteNonQueryAsync();
            using (var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.CheckConstraints, transaction))
            {
                bulk.DestinationTableName = "#FBImportParents";
                bulk.BulkCopyTimeout = 120;
                bulk.ColumnMappings.Add("ClientId", "ClientId");
                bulk.ColumnMappings.Add("LegacyFamilyId", "LegacyFamilyId");
                bulk.ColumnMappings.Add("Disposition", "Disposition");
                await bulk.WriteToServerAsync(rows);
            }
            await using var check = target.CreateCommand();
            check.Transaction = transaction;
            check.CommandTimeout = 120;
            check.CommandText = """
                SELECT COUNT_BIG(CASE WHEN p.FBClientsId IS NULL OR p.ModuleId IS NULL OR p.ModuleId<>@destinationModuleId
                    OR p.LegacyClientId IS NULL OR p.LegacyClientId<>r.ClientId
                    OR p.LegacyModuleId IS NULL OR p.LegacyModuleId<>@sourceModuleId THEN 1 END),
                    COUNT_BIG(CASE WHEN r.Disposition=@linked AND (f.FBClientsAddFamMemId IS NULL
                    OR f.FBClientsId IS NULL OR f.FBClientsId<>r.ClientId OR f.LegacyClAddFamMemId IS NULL
                    OR f.LegacyClAddFamMemId<>r.LegacyFamilyId) THEN 1 END)
                FROM #FBImportParents r
                LEFT JOIN dbo.GIBSFBClients p WITH (UPDLOCK,HOLDLOCK) ON p.FBClientsId=r.ClientId
                LEFT JOIN dbo.GIBSFBClientsAddFamMem f WITH (UPDLOCK,HOLDLOCK)
                    ON r.Disposition=@linked AND f.FBClientsAddFamMemId=r.LegacyFamilyId
                """;
            check.Parameters.Add("@destinationModuleId", SqlDbType.Int).Value = destinationModuleId;
            check.Parameters.Add("@sourceModuleId", SqlDbType.Int).Value = sourceModuleId;
            check.Parameters.Add("@linked", SqlDbType.Int).Value = (int)FamilyDisposition.Linked;
            await using (var reader = await check.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync()) throw new InvalidOperationException("Parent validation returned no result.");
                var invalidClients = reader.GetInt64(0);
                var invalidFamilies = reader.GetInt64(1);
                if (invalidClients > 0 || invalidFamilies > 0)
                    throw new ValidationException($"The batch has {invalidClients:N0} missing or mismatched destination clients and {invalidFamilies:N0} missing or mismatched destination family links. No batch changes were committed.");
            }
            await using var drop = target.CreateCommand();
            drop.Transaction = transaction;
            drop.CommandText = "DROP TABLE #FBImportParents";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
