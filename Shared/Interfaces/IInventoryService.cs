using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.Inventory.Services
{
    public interface IInventoryService
    {
        Task<InventoryCatalog> GetCatalogAsync(int moduleId);
        Task<InventoryPage<InventoryInvoice>> GetInvoicesAsync(int moduleId, int page, string search);
        Task<InventoryInvoiceDetails> GetInvoiceAsync(int moduleId, int invoiceId);
        Task<List<InventoryReportRow>> GetReportAsync(int moduleId, DateOnly start, DateOnly end);
        Task<int> SaveCategoryAsync(int moduleId, InventoryCategory input);
        Task<int> SaveProductAsync(int moduleId, InventoryProduct input);
        Task<int> SaveSupplierAsync(int moduleId, InventorySupplier input);
        Task<int> SaveInvoiceAsync(int moduleId, InventoryInvoice input);
        Task<int> SaveLineAsync(int moduleId, int invoiceId, InventoryLine input);
        Task DeleteInvoiceAsync(int moduleId, int invoiceId);
        Task DeleteLineAsync(int moduleId, int invoiceId, int lineId);
    }

    public record InventoryCatalog(List<InventoryCategory> Categories, List<InventoryProduct> Products, List<InventorySupplier> Suppliers);
    public record InventoryPage<T>(List<T> Items, int Total);
    public record InventoryInvoiceDetails(InventoryInvoice Invoice, List<InventoryLine> Lines);

    public record InventoryCategory
    {
        public int Id { get; set; }
        [Required, StringLength(50)] public string Name { get; set; }
        public int? SortOrder { get; set; }
        public string Instructions { get; set; }
        public bool Active { get; set; } = true;
    }

    public record InventoryProduct
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; }
        public int? CategoryId { get; set; }
        public decimal? CasePrice { get; set; }
        [Range(0, int.MaxValue)] public int? CaseCount { get; set; }
        public decimal? CaseWeight { get; set; }
        [Range(0, 100)] public int? Limit { get; set; } = 1;
        [StringLength(50)] public string LimitQuantities { get; set; }
        public bool Active { get; set; } = true;
    }

    public record InventorySupplier
    {
        public int Id { get; set; }
        [Required, StringLength(50)] public string Name { get; set; }
        [StringLength(50)] public string Address { get; set; }
        [StringLength(50)] public string City { get; set; }
        [StringLength(50)] public string State { get; set; }
        [StringLength(50)] public string Zip { get; set; }
        [StringLength(50)] public string Phone { get; set; }
        [StringLength(50)] public string Contact { get; set; }
        [StringLength(50)] public string ContactPhone { get; set; }
        public bool GBFB { get; set; }
        public bool Active { get; set; } = true;
    }

    public record InventoryInvoice
    {
        public int Id { get; set; }
        [Required, StringLength(50)] public string Number { get; set; }
        [Required] public DateOnly? Date { get; set; }
        [Required] public int? SupplierId { get; set; }
        [Required, StringLength(50)] public string Organization { get; set; }
    }

    public record InventoryLine
    {
        public int Id { get; set; }
        [Range(1, int.MaxValue)] public int ProductId { get; set; }
        [Required, Range(0, int.MaxValue)] public int? Cases { get; set; }
        [Required, Range(0, int.MaxValue)] public int? CountPerCase { get; set; }
        [Required] public decimal? PricePerCase { get; set; }
        [Required] public decimal? WeightPerCase { get; set; }
        [Required, StringLength(20)] public string ReportType { get; set; }
        public long Units => (long)Cases.GetValueOrDefault() * CountPerCase.GetValueOrDefault();
        public decimal Cost => Cases.GetValueOrDefault() * PricePerCase.GetValueOrDefault();
        public decimal Weight => Cases.GetValueOrDefault() * WeightPerCase.GetValueOrDefault();
    }

    public record InventoryReportRow(int InvoiceId, string Number, DateTime Date, string Supplier, string Category,
        string Product, string ReportType, string Organization, int Cases, int CountPerCase, decimal WeightPerCase, decimal PricePerCase)
    {
        public decimal Weight => Cases * WeightPerCase;
        public decimal Cost => Cases * PricePerCase;
    }

    public sealed class InventorySaveException(Exception innerException)
        : Exception("The change could not be saved. Reopen the record and check for related changes.", innerException);
}
