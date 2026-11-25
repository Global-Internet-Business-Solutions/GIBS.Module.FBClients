#nullable enable

using System.ComponentModel.DataAnnotations;
using Oqtane.Models;

namespace GIBS.Module.FBClients.Inventory.Models
{
    public class FBLineItem : ModelBase
    {
        [Key]
        public int LineItemId { get; set; }
        public int? LegacyLineItemId { get; set; }
        public int InvoiceId { get; set; }
        public int ProductId { get; set; }
        public int? Cases { get; set; } = 0;
        public int? CountPerCase { get; set; } = 0;
        public decimal? PricePerCase { get; set; } = 0;
        public decimal? WeightPerCase { get; set; }
        public decimal? GIBSFBLineItems { get; set; }
        [MaxLength(20)]
        public string? ReportType { get; set; }
    }
}
