using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GIBS.Module.FBClients.ImportData
{
    internal static class InventoryImportParentChecks
    {
        internal enum ParentKind { Category, Supplier, Product, Invoice, Visit }
        internal record ParentReference(ParentKind Kind, int Id, int? ClientId = null);

        internal static async Task ValidateAsync(SqlConnection target, SqlTransaction transaction,
            int inventoryModuleId, int clientsModuleId, int sourceClientsModuleId,
            IReadOnlyCollection<ParentReference> references, CancellationToken cancellationToken)
        {
            if (target == null || target.State != ConnectionState.Open || transaction == null || transaction.Connection != target
                || transaction.IsolationLevel != IsolationLevel.Serializable || inventoryModuleId <= 0 || clientsModuleId <= 0
                || sourceClientsModuleId <= 0 || references == null)
                throw new ValidationException("Inventory parent validation requires an open serializable destination transaction and valid module IDs.");

            foreach (var reference in references.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reference == null || reference.Id <= 0 || !Enum.IsDefined(reference.Kind)
                    || (reference.Kind == ParentKind.Visit ? reference.ClientId is null or <= 0 : reference.ClientId != null))
                    throw new ValidationException("An inventory batch contains an invalid parent reference.");
                await using var command = target.CreateCommand();
                command.Transaction = transaction;
                command.CommandTimeout = 120;
                command.CommandText = reference.Kind switch
                {
                    ParentKind.Category => "SELECT COUNT_BIG(*) FROM dbo.GIBSFBProductCategory WITH (UPDLOCK,HOLDLOCK) WHERE ProductCategoryId=@id AND LegacyProductCategoryId=@id AND ModuleId=@inventory",
                    ParentKind.Supplier => "SELECT COUNT_BIG(*) FROM dbo.GIBSFBSuppliers WITH (UPDLOCK,HOLDLOCK) WHERE SupplierId=@id AND LegacySupplierId=@id AND ModuleId=@inventory",
                    ParentKind.Product => "SELECT COUNT_BIG(*) FROM dbo.GIBSFBProducts WITH (UPDLOCK,HOLDLOCK) WHERE ProductId=@id AND LegacyProductId=@id AND ModuleId=@inventory",
                    ParentKind.Invoice => "SELECT COUNT_BIG(*) FROM dbo.GIBSFBInvoices WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceId=@id AND LegacyInvoiceId=@id AND ModuleId=@inventory",
                    ParentKind.Visit => "SELECT COUNT_BIG(*) FROM dbo.GIBSFBClientsVisits v WITH (UPDLOCK,HOLDLOCK) JOIN dbo.GIBSFBClients c WITH (UPDLOCK,HOLDLOCK) ON c.FBClientsId=v.FBClientsId WHERE v.VisitId=@id AND v.LegacyVisitId=@id AND v.FBClientsId=@client AND c.LegacyClientId=@client AND c.LegacyModuleId=@sourceClients AND c.ModuleId=@clients",
                    _ => throw new ValidationException("Unsupported inventory parent relationship.")
                };
                command.Parameters.Add("@id", SqlDbType.Int).Value = reference.Id;
                command.Parameters.Add("@inventory", SqlDbType.Int).Value = inventoryModuleId;
                command.Parameters.Add("@clients", SqlDbType.Int).Value = clientsModuleId;
                command.Parameters.Add("@sourceClients", SqlDbType.Int).Value = sourceClientsModuleId;
                command.Parameters.Add("@client", SqlDbType.Int).Value = (object)reference.ClientId ?? DBNull.Value;
                if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) != 1)
                    throw new ValidationException($"The destination {reference.Kind} parent {reference.Id} is missing, belongs to another module, or does not preserve its legacy identity. No batch changes were committed.");
            }
        }
    }
}
