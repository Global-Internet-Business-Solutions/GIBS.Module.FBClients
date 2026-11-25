using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Inventory;
using GIBS.Module.FBClients.Models;
using GIBS.Module.FBClients.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Oqtane.Infrastructure;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Ordering.Services
{
    public class ServerFoodOrderService(
        IDbContextFactory<FBClientsContext> factory, ITenantManager tenants, IModuleRepository modules,
        ISettingRepository settings, IUserPermissions permissions, IHttpContextAccessor http,
        FoodOrderLinks links) : IFoodOrderService
    {
        private const string Definition = "GIBS.Module.FBClients.Ordering, GIBS.Module.FBClients.Client.Oqtane";
        private const int MaxSelections = 500;

        internal async Task<int> GetOrderableInventoryAsync(int siteId, int moduleId, int clientId)
        {
            var inventoryId = GetInventoryModuleId(siteId, moduleId);
            if (inventoryId == 0) return 0;

            await using var db = factory.CreateDbContext();
            var householdSize = 1 + await db.FBClientsAddFamMem.CountAsync(member => member.FBClientsId == clientId);
            var sections = await ReadCatalogAsync(db, inventoryId, householdSize);
            return sections.Any(section => section.Products.Count > 0) ? inventoryId : 0;
        }

        public async Task<FoodOrderSheet> OpenAsync(int moduleId, string key)
        {
            var access = Authorize(moduleId, key);
            if (access == null) return null;

            await using var db = factory.CreateDbContext();
            return await LoadAsync(db, access);
        }

        public async Task<FoodOrderSubmitResult> SubmitAsync(int moduleId, string key, Dictionary<int, int> quantities)
        {
            var access = Authorize(moduleId, key);
            if (access == null) return FoodOrderSubmitResult.Unavailable;
            if (quantities == null || quantities.Count == 0 || quantities.Count > MaxSelections)
                return FoodOrderSubmitResult.InvalidQuantities;

            await using var db = factory.CreateDbContext();
            var sheet = await LoadAsync(db, access);
            if (sheet == null) return FoodOrderSubmitResult.Unavailable;

            if (!ValidQuantities(sheet.Sections, quantities)) return FoodOrderSubmitResult.InvalidQuantities;

            await using var transaction = await db.Database.BeginTransactionAsync();
            var changed = await EligibleVisits(db, access).ExecuteUpdateAsync(update => update
                .SetProperty(visit => visit.OrderStatusCode, (int?)2));
            if (changed != 1) return FoodOrderSubmitResult.Unavailable;

            db.FBClientsVisitItems.AddRange(quantities.Select(pair => new FBClientsVisitItem
            {
                VisitId = access.VisitId,
                ProductId = pair.Key,
                Quantity = pair.Value
            }));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return FoodOrderSubmitResult.Submitted;
        }

        private int GetInventoryModuleId(int siteId, int orderingModuleId)
        {
            var ordering = modules.GetModule(orderingModuleId);
            if (ordering == null || ordering.SiteId != siteId || ordering.ModuleDefinitionName != Definition) return 0;

            var value = settings.GetSettings(EntityNames.Module, orderingModuleId)
                .FirstOrDefault(setting => setting.SettingName == InventoryModule.OrderingSettingName)?.SettingValue;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var inventoryModuleId) || inventoryModuleId <= 0)
                return 0;

            var inventory = modules.GetModule(inventoryModuleId);
            return inventory != null && inventory.SiteId == siteId && inventory.ModuleDefinitionName == InventoryModule.DefinitionName
                ? inventoryModuleId : 0;
        }

        private FoodOrderAccess Authorize(int moduleId, string key)
        {
            var alias = tenants.GetAlias();
            if (alias == null) return null;

            var access = links.Read(key, alias.TenantId, alias.SiteId, moduleId);
            return access != null && access.InventoryModuleId == GetInventoryModuleId(alias.SiteId, moduleId)
                && permissions.IsAuthorized(http.HttpContext?.User, alias.SiteId, EntityNames.Module, moduleId, PermissionNames.View)
                ? access : null;
        }

        internal static bool ValidQuantities(IEnumerable<FoodOrderSection> sections, Dictionary<int, int> quantities)
        {
            if (quantities == null || quantities.Count == 0 || quantities.Count > MaxSelections) return false;
            var limits = sections.SelectMany(section => section.Products).ToDictionary(product => product.Id, product => product.Limit);
            return quantities.All(pair => limits.TryGetValue(pair.Key, out var limit) && pair.Value >= 1 && pair.Value <= limit);
        }

        internal static async Task<List<FoodOrderSection>> ReadCatalogAsync(FBClientsContext db, int moduleId, int householdSize)
        {
            var rows = await (from category in db.FBProductCategories
                              where category.ModuleId == moduleId && category.IsActive
                              join product in db.FBProducts.Where(product => product.ModuleId == moduleId && product.IsActive)
                                  on (int?)category.ProductCategoryId equals product.ProductCategoryId into products
                              from product in products.DefaultIfEmpty()
                              orderby category.SortOrder, category.ProductCategory, category.ProductCategoryId, product.ProductName, product.ProductId
                              select new
                              {
                                  category.ProductCategoryId,
                                  category.ProductCategory,
                                  category.OrderingInstructions,
                                  ProductId = (int?)product.ProductId,
                                  product.ProductName,
                                  product.Limit,
                                  product.LimitQuantities
                              }).ToListAsync();

            return rows.GroupBy(row => row.ProductCategoryId).Select(category =>
            {
                var details = category.First();
                var choices = category.Where(row => row.ProductId.HasValue && !string.IsNullOrWhiteSpace(row.ProductName))
                    .Select(row => new FoodOrderChoice(row.ProductId.Value, row.ProductName,
                        InventoryOrderLimit.Resolve(row.Limit, row.LimitQuantities, householdSize)))
                    .Where(product => product.Limit > 0).ToList();
                return new FoodOrderSection(details.ProductCategory, details.OrderingInstructions, choices);
            }).ToList();
        }

        private static IQueryable<FBClientsVisits> EligibleVisits(FBClientsContext db, FoodOrderAccess access)
        {
            return db.FBClientsVisits.Where(visit => visit.VisitId == access.VisitId && visit.FBClientsId == access.ClientId
                && (visit.OrderStatusCode == null || visit.OrderStatusCode == 0 || visit.OrderStatusCode == 1)
                && db.FBClients.Any(client => client.FBClientsId == visit.FBClientsId && client.ModuleId == access.ClientModuleId
                    && client.IsLocked != true));
        }

        private async Task<FoodOrderSheet> LoadAsync(FBClientsContext db, FoodOrderAccess access)
        {
            var sheet = await (from visit in EligibleVisits(db, access)
                               join client in db.FBClients on visit.FBClientsId equals client.FBClientsId
                               select new FoodOrderSheet
                               {
                                   VisitDate = visit.VisitDate,
                                   ClientName = (client.ClientFirstName ?? "") + " " + (client.ClientLastName ?? ""),
                                   BagsAllowed = visit.VisitNumBags,
                                   HouseholdSize = 1 + db.FBClientsAddFamMem.Count(member => member.FBClientsId == client.FBClientsId)
                               }).SingleOrDefaultAsync();
            if (sheet == null) return null;

            sheet.Sections = await ReadCatalogAsync(db, access.InventoryModuleId, sheet.HouseholdSize);
            return sheet.Sections.Any(section => section.Products.Count > 0) ? sheet : null;
        }
    }
}
