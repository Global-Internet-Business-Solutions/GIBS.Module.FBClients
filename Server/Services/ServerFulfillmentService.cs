using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Inventory;
using GIBS.Module.FBClients.Models;
using GIBS.Module.FBClients.Ordering.Services;
using GIBS.Module.FBClients.Repository;
using GIBS.Module.FBClients.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Fulfillment
{
    public class ServerFulfillmentService(IDbContextFactory<FBClientsContext> factory, ITenantManager tenants,
        IModuleRepository modules, IPageModuleRepository pages, ISettingRepository settings, IUserPermissions permissions,
        IHttpContextAccessor http, AuthenticationStateProvider authentication, IFBClientsOrderTextSender sender, ILogManager logger) : IFulfillmentService
    {
        private record Scope(int SiteId, int ClientsId, int InventoryId, bool CanEdit, bool Texts, ClaimsPrincipal User);
        private record VisitData(FulfillmentVisit Visit, int Household, string Notes, string Cell);

        private async Task<Scope> GetScopeAsync(int moduleId, bool edit = false, bool inventory = false)
        {
            var alias = tenants.GetAlias();
            var user = http.HttpContext?.User ?? (await authentication.GetAuthenticationStateAsync()).User;
            if (alias == null || user.Identity?.IsAuthenticated != true || !Matches(moduleId, alias.SiteId, FulfillmentModule.DefinitionName))
                throw new UnauthorizedAccessException();

            bool Allowed(int id, string permission) => permissions.IsAuthorized(user, alias.SiteId, EntityNames.Module, id, permission);
            var canEdit = Allowed(moduleId, PermissionNames.Edit);
            if (!Allowed(moduleId, PermissionNames.View) || (edit && !canEdit)) throw new UnauthorizedAccessException();
            var values = settings.GetSettings(EntityNames.Module, moduleId).ToDictionary(x => x.SettingName, x => x.SettingValue);
            int ReadId(string name) => values.TryGetValue(name, out var value) && int.TryParse(value, NumberStyles.None,
                CultureInfo.InvariantCulture, out var id) ? id : 0;
            var clientsId = ReadId(FulfillmentModule.ClientsModuleId);
            var inventoryId = ReadId(InventoryModule.OrderingSettingName);
            if (!Matches(clientsId, alias.SiteId, FulfillmentModule.ClientsDefinitionName))
                throw new ValidationException("Set the ID of an FBClients module on this site in Fulfillment Settings.");
            if (!Allowed(clientsId, PermissionNames.View)) throw new UnauthorizedAccessException();
            var texts = values.TryGetValue(FulfillmentModule.PickupTexts, out var enabled) && bool.TryParse(enabled, out var flag) && flag;
            var scope = new Scope(alias.SiteId, clientsId, inventoryId, canEdit, texts, user);
            if (inventory)
            {
                var access = GetCatalogAccess(scope);
                if (access == FulfillmentCatalogAccess.NotConfigured)
                    throw new ValidationException("Set the ID of an FBClients Inventory module on this site in Fulfillment Settings.");
                if (access == FulfillmentCatalogAccess.Denied) throw new UnauthorizedAccessException();
            }
            return scope;
        }

        private FulfillmentCatalogAccess GetCatalogAccess(Scope scope)
        {
            if (!Matches(scope.InventoryId, scope.SiteId, InventoryModule.DefinitionName)) return FulfillmentCatalogAccess.NotConfigured;
            return permissions.IsAuthorized(scope.User, scope.SiteId, EntityNames.Module, scope.InventoryId, PermissionNames.View)
                ? FulfillmentCatalogAccess.Available : FulfillmentCatalogAccess.Denied;
        }

        private bool Matches(int id, int siteId, string definition) => id > 0 && modules.GetModule(id) is { } module
            && !module.IsDeleted && module.SiteId == siteId && module.ModuleDefinitionName == definition;

        private static IQueryable<FBClientsVisits> Visits(FBClientsContext db, int clientsId) => db.FBClientsVisits
            .Where(visit => db.FBClients.Any(client => client.FBClientsId == visit.FBClientsId && client.ModuleId == clientsId));

        private static IQueryable<FBClientsVisits> Unlocked(FBClientsContext db, int clientsId, int visitId) => db.FBClientsVisits
            .Where(visit => visit.VisitId == visitId && db.FBClients.Any(client => client.FBClientsId == visit.FBClientsId
                && client.ModuleId == clientsId && client.IsLocked != true));

        public async Task<FulfillmentQueue> GetQueueAsync(int moduleId, DateOnly date, int? status, string location, int page)
        {
            if (date == default || (status.HasValue && !Enum.IsDefined((FoodOrderStatus)status.Value))
                || location?.Length > 50 || page < 0 || page > int.MaxValue / FulfillmentModule.PageSize)
                throw new ValidationException("Enter a valid visit date, status, location, and page.");
            var scope = await GetScopeAsync(moduleId);
            await using var db = factory.CreateDbContext();
            var daily = Visits(db, scope.ClientsId).Where(x => x.VisitDate == date);
            var locations = await daily.Where(x => x.ServiceLocation != "").Select(x => x.ServiceLocation).Distinct().OrderBy(x => x).ToListAsync();
            var filtered = daily;
            if (status.HasValue) filtered = filtered.Where(x => (x.OrderStatusCode ?? 0) == status.Value);
            if (!string.IsNullOrWhiteSpace(location)) filtered = filtered.Where(x => x.ServiceLocation == location);
            var total = await filtered.CountAsync();
            var rows = await (from visit in filtered
                              join client in db.FBClients on visit.FBClientsId equals client.FBClientsId
                              orderby visit.VisitId descending
                              select new
                              {
                                  visit.VisitId, visit.FBClientsId, client.ClientFirstName, client.ClientLastName, visit.VisitDate,
                                  visit.LegacyCreatedOnDate, visit.CreatedOn, visit.VisitNumBags, visit.ServiceLocation,
                                  visit.OrderStatusCode, client.IsLocked, client.ClientPhone, client.ClientPhoneType
                              }).Skip(page * FulfillmentModule.PageSize).Take(FulfillmentModule.PageSize).ToListAsync();
            var result = rows.Select(x => new FulfillmentVisit(x.VisitId, x.FBClientsId, Name(x.ClientFirstName, x.ClientLastName),
                x.VisitDate, x.LegacyCreatedOnDate ?? x.CreatedOn, x.VisitNumBags, x.ServiceLocation, x.OrderStatusCode ?? 0,
                x.IsLocked == true, scope.CanEdit && scope.Texts && x.IsLocked != true && x.OrderStatusCode == (int)FoodOrderStatus.Filled
                    && CellNumber(x.ClientPhoneType, x.ClientPhone) != null)).ToList();
            var path = permissions.IsAuthorized(scope.User, scope.SiteId, EntityNames.Module, scope.ClientsId, PermissionNames.Edit)
                ? pages.GetPageModules(scope.SiteId).Where(x => x.ModuleId == scope.ClientsId && !x.IsDeleted && x.Page != null && !x.Page.IsDeleted)
                    .OrderBy(x => x.PageModuleId).Where(x => permissions.IsAuthorized(scope.User, scope.SiteId, EntityNames.Page, x.PageId, PermissionNames.Edit))
                    .Select(x => x.Page.Path).FirstOrDefault()
                : null;
            return new(result, total, locations, scope.ClientsId, path);
        }

        public async Task<FulfillmentOrder> GetOrderAsync(int moduleId, int visitId)
        {
            var scope = await GetScopeAsync(moduleId);
            await using var db = factory.CreateDbContext();
            var data = await ReadVisitAsync(db, scope, visitId);
            var catalogAccess = GetCatalogAccess(scope);
            var items = db.FBClientsVisitItems.Where(item => item.VisitId == visitId);
            if (catalogAccess != FulfillmentCatalogAccess.Available)
            {
                var saved = await items.OrderBy(item => item.VisitItemId)
                    .Select(item => new { item.VisitItemId, item.ProductId, item.Quantity }).ToListAsync();
                return new(data.Visit, data.Household, data.Notes,
                    saved.Select(item => new FulfillmentLine(item.VisitItemId, null, $"Product #{item.ProductId}", item.Quantity)).ToList(),
                    [], catalogAccess);
            }

            var lines = await (from item in items
                               join product in db.FBProducts.Where(x => x.ModuleId == scope.InventoryId)
                                   on item.ProductId equals product.ProductId into products
                               from product in products.DefaultIfEmpty()
                               join category in db.FBProductCategories.Where(x => x.ModuleId == scope.InventoryId)
                                   on product.ProductCategoryId equals (int?)category.ProductCategoryId into categories
                               from category in categories.DefaultIfEmpty()
                               orderby category.SortOrder, category.ProductCategory, product.ProductName, item.VisitItemId
                               select new { item.VisitItemId, item.ProductId, item.Quantity, category.ProductCategory, product.ProductName }).ToListAsync();
            var sections = scope.CanEdit && !data.Visit.Locked && lines.Count == 0 && data.Visit.Status == (int)FoodOrderStatus.Paper
                ? await ServerFoodOrderService.ReadCatalogAsync(db, scope.InventoryId, data.Household) : [];
            return new(data.Visit, data.Household, data.Notes,
                lines.Select(x => new FulfillmentLine(x.VisitItemId, x.ProductCategory, x.ProductName ?? $"Product #{x.ProductId}", x.Quantity)).ToList(), sections, catalogAccess);
        }

        public async Task SubmitPaperOrderAsync(int moduleId, int visitId, Dictionary<int, int> quantities)
        {
            var scope = await GetScopeAsync(moduleId, edit: true, inventory: true);
            await using var db = factory.CreateDbContext();
            var data = await ReadVisitAsync(db, scope, visitId);
            if (data.Visit.Locked || data.Visit.Status != (int)FoodOrderStatus.Paper) throw Changed();
            var sections = await ServerFoodOrderService.ReadCatalogAsync(db, scope.InventoryId, data.Household);
            if (!ServerFoodOrderService.ValidQuantities(sections, quantities))
                throw new ValidationException("Select at least one product and use the current quantity limits for this household.");

            await using var tx = await db.Database.BeginTransactionAsync();
            var changed = await Unlocked(db, scope.ClientsId, visitId)
                .Where(x => (x.OrderStatusCode ?? 0) == (int)FoodOrderStatus.Paper && !db.FBClientsVisitItems.Any(item => item.VisitId == x.VisitId))
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.OrderStatusCode, (int?)FoodOrderStatus.Submitted));
            if (changed != 1) throw Changed();
            db.FBClientsVisitItems.AddRange(quantities.Select(x => new FBClientsVisitItem { VisitId = visitId, ProductId = x.Key, Quantity = x.Value }));
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task MarkFilledAsync(int moduleId, int visitId, int[] checkedItems)
        {
            var scope = await GetScopeAsync(moduleId, edit: true, inventory: true);
            await using var db = factory.CreateDbContext();
            var data = await ReadVisitAsync(db, scope, visitId);
            if (data.Visit.Locked || data.Visit.Status != (int)FoodOrderStatus.Submitted) throw Changed();
            var items = db.FBClientsVisitItems.Where(x => x.VisitId == visitId);
            var ids = await items.Select(x => x.VisitItemId).ToListAsync();
            if (ids.Count == 0 || checkedItems == null || !ids.ToHashSet().SetEquals(checkedItems))
                throw new ValidationException("Check every item in the order before marking it filled.");
            var changed = await Unlocked(db, scope.ClientsId, visitId).Where(x => x.OrderStatusCode == (int)FoodOrderStatus.Submitted
                && items.Count() == ids.Count && !items.Any(item => !ids.Contains(item.VisitItemId)))
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.OrderStatusCode, (int?)FoodOrderStatus.Filled));
            if (changed != 1) throw Changed();
        }

        public async Task<FBClientsOrderTextResult> SendPickupTextAsync(int moduleId, int visitId)
        {
            var scope = await GetScopeAsync(moduleId, edit: true);
            if (!scope.Texts) throw new ValidationException("Enable pickup texts in Fulfillment Settings first.");
            await using var db = factory.CreateDbContext();
            var data = await ReadVisitAsync(db, scope, visitId);
            if (data.Visit.Locked || data.Visit.Status != (int)FoodOrderStatus.Filled) throw Changed();
            var config = FBClientsSettings.FromDictionary(settings.GetSettings(EntityNames.Module, scope.ClientsId)
                .ToDictionary(x => x.SettingName, x => x.SettingValue));
            var from = CellNumber("Cell", config.TwilioPhoneNumber);
            if (from == null || string.IsNullOrWhiteSpace(config.TwilioAccountSid) || string.IsNullOrWhiteSpace(config.TwilioAuthToken))
                throw new ValidationException("Configure the Twilio credentials and sender number in the linked FBClients module.");
            if (data.Cell == null) throw new ValidationException("Save a valid cell number on the client record before sending a pickup text.");

            // Hold the conditional status update until acceptance so another request cannot send the same text concurrently.
            await using var tx = await db.Database.BeginTransactionAsync();
            var changed = await Unlocked(db, scope.ClientsId, visitId).Where(x => x.OrderStatusCode == (int)FoodOrderStatus.Filled)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.OrderStatusCode, (int?)FoodOrderStatus.PickupTextSent));
            if (changed != 1) throw Changed();

            string sid;
            try
            {
                var body = string.IsNullOrWhiteSpace(config.FBName) ? "Your order is ready to pick up."
                    : $"{config.FBName.Trim()}: Your order is ready to pick up.";
                sid = await sender.SendAsync(config.TwilioAccountSid.Trim(), config.TwilioAuthToken.Trim(), from, data.Cell, body);
                if (string.IsNullOrWhiteSpace(sid)) throw new InvalidOperationException("Twilio supplied no message SID.");
            }
            catch (Twilio.Exceptions.ApiException ex)
            {
                logger.Log(LogLevel.Warning, this, LogFunction.Other, "Pickup text rejected for visit {VisitId}: code {Code}", visitId, ex.Code);
                return new() { ErrorMessage = $"Twilio rejected the pickup text (code {ex.Code}). Check your Twilio console before retrying." };
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Other, "Pickup text acceptance unconfirmed for visit {VisitId}: {ErrorType}", visitId, ex.GetType().Name);
                return new() { AcceptanceUnconfirmed = true, ErrorMessage = "Twilio acceptance could not be confirmed. Check the Twilio console before retrying." };
            }

            try
            {
                await tx.CommitAsync();
                logger.Log(LogLevel.Information, this, LogFunction.Other, "Pickup text accepted for visit {VisitId}: {MessageSid}", visitId, sid);
                return new() { Accepted = true, StatusUpdated = true, MessageSid = sid };
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, this, LogFunction.Update, "Pickup status commit failed for visit {VisitId}: {MessageSid} {ErrorType}", visitId, sid, ex.GetType().Name);
                return new() { Accepted = true, MessageSid = sid, ErrorMessage = "Twilio accepted the text, but saving the status could not be confirmed. Do not resend." };
            }
        }

        private static async Task<VisitData> ReadVisitAsync(FBClientsContext db, Scope scope, int visitId)
        {
            var x = await (from visit in Visits(db, scope.ClientsId)
                           where visit.VisitId == visitId
                           join client in db.FBClients on visit.FBClientsId equals client.FBClientsId
                           select new
                           {
                               visit.VisitId, visit.FBClientsId, client.ClientFirstName, client.ClientLastName, visit.VisitDate,
                               visit.LegacyCreatedOnDate, visit.CreatedOn, visit.VisitNumBags, visit.ServiceLocation,
                               visit.OrderStatusCode, client.IsLocked, client.ClientPhone, client.ClientPhoneType, visit.VisitNotes,
                               Household = 1 + db.FBClientsAddFamMem.Count(member => member.FBClientsId == client.FBClientsId)
                           }).SingleOrDefaultAsync() ?? throw new KeyNotFoundException("The visit was not found in the linked FBClients module.");
            var cell = CellNumber(x.ClientPhoneType, x.ClientPhone);
            return new(new(x.VisitId, x.FBClientsId, Name(x.ClientFirstName, x.ClientLastName), x.VisitDate, x.LegacyCreatedOnDate ?? x.CreatedOn,
                x.VisitNumBags, x.ServiceLocation, x.OrderStatusCode ?? 0, x.IsLocked == true,
                scope.CanEdit && scope.Texts && x.IsLocked != true && x.OrderStatusCode == (int)FoodOrderStatus.Filled && cell != null), x.Household, x.VisitNotes, cell);
        }

        private static string Name(string first, string last) => $"{first} {last}".Trim();
        private static FulfillmentConflictException Changed() => new("This visit is locked or the order changed. Refresh the queue before continuing.");
        private static string CellNumber(string type, string phone)
        {
            if (!string.Equals(type, "Cell", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(phone)) return null;
            try { return FBClientsOrderText.NormalizePhoneNumber(phone); }
            catch (ArgumentException) { return null; }
        }
    }
}
