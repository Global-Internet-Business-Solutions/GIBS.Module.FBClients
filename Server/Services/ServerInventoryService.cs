using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Inventory.Models;
using GIBS.Module.FBClients.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Oqtane.Infrastructure;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Inventory.Services
{
    public class ServerInventoryService(IDbContextFactory<FBClientsContext> factory, ITenantManager tenants,
        IModuleRepository modules, IUserPermissions permissions, IHttpContextAccessor http) : IInventoryService
    {
        private FBClientsContext Open(int moduleId, string permission = PermissionNames.View)
        {
            var alias = tenants.GetAlias();
            var user = http.HttpContext?.User;
            var module = alias == null || user?.Identity?.IsAuthenticated != true ? null : modules.GetModule(moduleId);
            if (module == null || module.SiteId != alias.SiteId || module.ModuleDefinitionName != InventoryModule.DefinitionName
                || !permissions.IsAuthorized(user, alias.SiteId, EntityNames.Module, moduleId, permission))
                throw new UnauthorizedAccessException();
            return factory.CreateDbContext();
        }

        public async Task<InventoryCatalog> GetCatalogAsync(int moduleId)
        {
            await using var db = Open(moduleId);
            var categories = await db.FBProductCategories.Where(x => x.ModuleId == moduleId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.ProductCategory).ThenBy(x => x.ProductCategoryId)
                .Select(x => new InventoryCategory { Id = x.ProductCategoryId, Name = x.ProductCategory, Active = x.IsActive,
                    SortOrder = x.SortOrder, Instructions = x.OrderingInstructions }).ToListAsync();
            var products = await db.FBProducts.Where(x => x.ModuleId == moduleId)
                .OrderBy(x => x.ProductName).ThenBy(x => x.ProductId)
                .Select(x => new InventoryProduct { Id = x.ProductId, Name = x.ProductName, CategoryId = x.ProductCategoryId,
                    CaseCount = x.CaseCount, CasePrice = x.CasePrice, CaseWeight = x.CaseWeight, Limit = x.Limit,
                    LimitQuantities = x.LimitQuantities, Active = x.IsActive }).ToListAsync();
            var suppliers = await db.FBSuppliers.Where(x => x.ModuleId == moduleId)
                .OrderBy(x => x.SupplierName).ThenBy(x => x.SupplierId)
                .Select(x => new InventorySupplier { Id = x.SupplierId, Name = x.SupplierName, Address = x.Address, City = x.City,
                    State = x.State, Zip = x.Zip, Phone = x.SupplierPhone, Contact = x.Salesman, ContactPhone = x.SalesmanPhone,
                    GBFB = x.GBFB, Active = x.IsActive }).ToListAsync();
            return new InventoryCatalog(categories, products, suppliers);
        }

        public async Task<InventoryPage<InventoryInvoice>> GetInvoicesAsync(int moduleId, int page, string search)
        {
            await using var db = Open(moduleId);
            if (page < 0 || search?.Length > 100) throw new ValidationException("Invalid invoice page or search.");
            var query = db.FBInvoices.Where(x => x.ModuleId == moduleId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.InvoiceNumber.Contains(term) || x.Organization.Contains(term)
                    || db.FBSuppliers.Any(s => s.SupplierId == x.SupplierId && s.ModuleId == moduleId && s.SupplierName.Contains(term)));
            }
            var total = await query.CountAsync();
            var size = InventoryModule.InvoicePageSize;
            var items = page > Math.Max(0, total - 1) / size ? new List<InventoryInvoice>() : await query
                .OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.InvoiceId)
                .Skip(page * size).Take(size).Select(Invoice).ToListAsync();
            return new InventoryPage<InventoryInvoice>(items, total);
        }

        public async Task<InventoryInvoiceDetails> GetInvoiceAsync(int moduleId, int invoiceId)
        {
            await using var db = Open(moduleId);
            var invoice = await db.FBInvoices.Where(x => x.ModuleId == moduleId && x.InvoiceId == invoiceId)
                .Select(Invoice).SingleOrDefaultAsync() ?? throw Missing("Invoice");
            var lines = await db.FBLineItems.Where(x => x.InvoiceId == invoiceId && db.FBInvoices.Any(i => i.InvoiceId == x.InvoiceId && i.ModuleId == moduleId))
                .OrderBy(x => x.LineItemId).Select(x => new InventoryLine { Id = x.LineItemId, ProductId = x.ProductId,
                    Cases = x.Cases, CountPerCase = x.CountPerCase, PricePerCase = x.PricePerCase, WeightPerCase = x.WeightPerCase,
                    ReportType = x.ReportType }).ToListAsync();
            return new InventoryInvoiceDetails(invoice, lines);
        }

        public async Task<List<InventoryReportRow>> GetReportAsync(int moduleId, DateOnly start, DateOnly end)
        {
            await using var db = Open(moduleId);
            if (end < start || end.DayNumber - start.DayNumber > 366 || end == DateOnly.MaxValue)
                throw new ValidationException("Choose a date range of up to one year.");
            var fromDate = start.ToDateTime(TimeOnly.MinValue);
            var until = end.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var rows = await (from invoice in db.FBInvoices
                              where invoice.ModuleId == moduleId && invoice.InvoiceDate >= fromDate && invoice.InvoiceDate < until
                              join line in db.FBLineItems on invoice.InvoiceId equals line.InvoiceId
                              join supplier in db.FBSuppliers.Where(x => x.ModuleId == moduleId) on invoice.SupplierId equals (int?)supplier.SupplierId into suppliers
                              from supplier in suppliers.DefaultIfEmpty()
                              join product in db.FBProducts.Where(x => x.ModuleId == moduleId) on line.ProductId equals product.ProductId into products
                              from product in products.DefaultIfEmpty()
                              join category in db.FBProductCategories.Where(x => x.ModuleId == moduleId) on product.ProductCategoryId equals (int?)category.ProductCategoryId into categories
                              from category in categories.DefaultIfEmpty()
                              orderby invoice.InvoiceDate, invoice.InvoiceId, line.LineItemId
                              select new { invoice.InvoiceId, invoice.InvoiceNumber, invoice.InvoiceDate, supplier.SupplierName,
                                  category.ProductCategory, product.ProductName, line.ProductId, line.ReportType, invoice.Organization,
                                  line.Cases, line.CountPerCase, line.PricePerCase, line.WeightPerCase }).Take(10_001).ToListAsync();
            if (rows.Count > 10_000) throw new ValidationException("This report has more than 10,000 line items. Choose a shorter date range.");
            return rows.Select(x => new InventoryReportRow(x.InvoiceId, x.InvoiceNumber, x.InvoiceDate.Value, x.SupplierName,
                x.ProductCategory, x.ProductName ?? $"Product #{x.ProductId}", x.ReportType, x.Organization, x.Cases.GetValueOrDefault(),
                x.CountPerCase.GetValueOrDefault(), x.WeightPerCase.GetValueOrDefault(), x.PricePerCase.GetValueOrDefault())).ToList();
        }

        public async Task<int> SaveCategoryAsync(int moduleId, InventoryCategory input)
        {
            await using var db = Open(moduleId, PermissionNames.Edit);
            Validate(input);
            var row = input.Id == 0 ? new FBProductCategory { ModuleId = moduleId }
                : await db.FBProductCategories.SingleOrDefaultAsync(x => x.ProductCategoryId == input.Id && x.ModuleId == moduleId)
                    ?? throw Missing("Category");
            row.ProductCategory = input.Name.Trim();
            row.SortOrder = input.SortOrder;
            row.OrderingInstructions = Clean(input.Instructions);
            row.IsActive = input.Active;
            if (input.Id == 0) db.Add(row);
            await CommitAsync(db);
            return row.ProductCategoryId;
        }

        public async Task<int> SaveProductAsync(int moduleId, InventoryProduct input)
        {
            await using var db = Open(moduleId, PermissionNames.Edit);
            Validate(input);
            ValidateDecimal(input.CasePrice, 999_999_999_999_999.9999m, 4, "Case price");
            ValidateDecimal(input.CaseWeight, 9_999_999_999_999_999.99m, 2, "Case weight");
            var overrides = Clean(input.LimitQuantities);
            if (overrides != null)
            {
                var sizes = new HashSet<int>();
                foreach (var item in overrides.Split(',', StringSplitOptions.TrimEntries))
                {
                    var assignment = item.Split('=', StringSplitOptions.TrimEntries);
                    if (assignment.Length != 2 || !int.TryParse(assignment[0], NumberStyles.None, CultureInfo.InvariantCulture, out var size)
                        || size <= 0 || !sizes.Add(size) || !int.TryParse(assignment[1], NumberStyles.None, CultureInfo.InvariantCulture, out var quantity)
                        || quantity is < 1 or > 100)
                        throw new ValidationException("Use unique positive household sizes and quantities from 1 to 100, for example 4=3,5=4.");
                }
            }
            if (input.CategoryId.HasValue && !await db.FBProductCategories.AnyAsync(x => x.ProductCategoryId == input.CategoryId && x.ModuleId == moduleId))
                throw new ValidationException("Select a category from this inventory.");
            var row = input.Id == 0 ? new FBProduct { ModuleId = moduleId }
                : await db.FBProducts.SingleOrDefaultAsync(x => x.ProductId == input.Id && x.ModuleId == moduleId) ?? throw Missing("Product");
            row.ProductName = input.Name.Trim();
            row.ProductCategoryId = input.CategoryId;
            row.CasePrice = input.CasePrice;
            row.CaseCount = input.CaseCount;
            row.CaseWeight = input.CaseWeight;
            row.Limit = input.Limit;
            row.LimitQuantities = overrides;
            row.IsActive = input.Active;
            if (input.Id == 0) db.Add(row);
            await CommitAsync(db);
            return row.ProductId;
        }

        public async Task<int> SaveSupplierAsync(int moduleId, InventorySupplier input)
        {
            await using var db = Open(moduleId, PermissionNames.Edit);
            Validate(input);
            var row = input.Id == 0 ? new FBSupplier { ModuleId = moduleId }
                : await db.FBSuppliers.SingleOrDefaultAsync(x => x.SupplierId == input.Id && x.ModuleId == moduleId) ?? throw Missing("Supplier");
            row.SupplierName = input.Name.Trim();
            row.Address = Clean(input.Address);
            row.City = Clean(input.City);
            row.State = Clean(input.State);
            row.Zip = Clean(input.Zip);
            row.SupplierPhone = Clean(input.Phone);
            row.Salesman = Clean(input.Contact);
            row.SalesmanPhone = Clean(input.ContactPhone);
            row.GBFB = input.GBFB;
            row.IsActive = input.Active;
            if (input.Id == 0) db.Add(row);
            await CommitAsync(db);
            return row.SupplierId;
        }

        public async Task<int> SaveInvoiceAsync(int moduleId, InventoryInvoice input)
        {
            await using var db = Open(moduleId, PermissionNames.Edit);
            Validate(input);
            var row = input.Id == 0 ? new FBInvoice { ModuleId = moduleId } : await FindInvoice(db, moduleId, input.Id);
            if (!await db.FBSuppliers.AnyAsync(x => x.SupplierId == input.SupplierId && x.ModuleId == moduleId
                    && (x.IsActive || x.SupplierId == row.SupplierId)))
                throw new ValidationException("Select an active supplier from this inventory.");
            row.InvoiceNumber = input.Number.Trim();
            row.InvoiceDate = input.Date.Value.ToDateTime(TimeOnly.MinValue);
            row.SupplierId = input.SupplierId;
            row.Organization = input.Organization.Trim();
            if (input.Id == 0) db.Add(row);
            await CommitAsync(db);
            return row.InvoiceId;
        }

        public async Task<int> SaveLineAsync(int moduleId, int invoiceId, InventoryLine input)
        {
            await using var db = Open(moduleId, PermissionNames.Edit);
            Validate(input);
            ValidateDecimal(input.PricePerCase, 999_999_999_999_999.9999m, 4, "Price per case");
            ValidateDecimal(input.WeightPerCase, 9_999_999_999_999_999.99m, 2, "Weight per case");
            await FindInvoice(db, moduleId, invoiceId);
            var row = input.Id == 0 ? new FBLineItem { InvoiceId = invoiceId } : await FindLine(db, invoiceId, input.Id);
            if (!await db.FBProducts.AnyAsync(x => x.ProductId == input.ProductId && x.ModuleId == moduleId
                    && (x.IsActive || x.ProductId == row.ProductId)))
                throw new ValidationException("Select an active product from this inventory.");
            row.ProductId = input.ProductId;
            row.Cases = input.Cases;
            row.CountPerCase = input.CountPerCase;
            row.PricePerCase = input.PricePerCase;
            row.WeightPerCase = input.WeightPerCase;
            row.ReportType = input.ReportType.Trim();
            if (input.Id == 0) db.Add(row);
            await CommitAsync(db);
            return row.LineItemId;
        }

        public async Task DeleteInvoiceAsync(int moduleId, int invoiceId)
        {
            await using var db = Open(moduleId, PermissionNames.Edit);
            var row = await FindInvoice(db, moduleId, invoiceId);
            db.FBLineItems.RemoveRange(await db.FBLineItems.Where(x => x.InvoiceId == invoiceId).ToListAsync());
            db.Remove(row);
            await CommitAsync(db);
        }

        public async Task DeleteLineAsync(int moduleId, int invoiceId, int lineId)
        {
            await using var db = Open(moduleId, PermissionNames.Edit);
            await FindInvoice(db, moduleId, invoiceId);
            db.Remove(await FindLine(db, invoiceId, lineId));
            await CommitAsync(db);
        }

        private static async Task CommitAsync(FBClientsContext db)
        {
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InventorySaveException(ex);
            }
        }

        private static readonly Expression<Func<FBInvoice, InventoryInvoice>> Invoice = x => new InventoryInvoice
        {
            Id = x.InvoiceId, Number = x.InvoiceNumber, Organization = x.Organization, SupplierId = x.SupplierId,
            Date = x.InvoiceDate.HasValue ? DateOnly.FromDateTime(x.InvoiceDate.Value) : null
        };

        private static async Task<FBInvoice> FindInvoice(FBClientsContext db, int moduleId, int id) =>
            await db.FBInvoices.SingleOrDefaultAsync(x => x.InvoiceId == id && x.ModuleId == moduleId) ?? throw Missing("Invoice");

        private static async Task<FBLineItem> FindLine(FBClientsContext db, int invoiceId, int id) =>
            await db.FBLineItems.SingleOrDefaultAsync(x => x.LineItemId == id && x.InvoiceId == invoiceId) ?? throw Missing("Line item");

        private static void Validate(object input)
        {
            if (input == null) throw new ValidationException("Enter the inventory record details.");
            Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);
        }

        private static void ValidateDecimal(decimal? value, decimal maximum, int scale, string field)
        {
            if (value.HasValue && (value.Value < 0 || value.Value > maximum || decimal.Round(value.Value, scale) != value.Value))
                throw new ValidationException($"{field} must be a non-negative amount with at most {scale} decimal places that fits the database field.");
        }

        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static KeyNotFoundException Missing(string record) => new($"{record} was not found in this inventory.");
    }
}
