#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using GIBS.Module.FBClients.Models;

namespace GIBS.Module.FBClients.Inventory.Models
{
    public class FBInvoice : FBClientsLegacyRecord
    {
        [Key]
        public int InvoiceId { get; set; }
        public int? LegacyInvoiceId { get; set; }
        [MaxLength(50)]
        public string? InvoiceNumber { get; set; } = "0";
        public DateTime? InvoiceDate { get; set; }
        public int? SupplierId { get; set; }
        public int ModuleId { get; set; }
        public int? LegacyPortalId { get; set; }
        [MaxLength(50)]
        public string? Organization { get; set; }
    }
}
