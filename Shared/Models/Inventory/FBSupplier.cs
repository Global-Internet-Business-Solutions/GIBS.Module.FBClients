#nullable enable

using System.ComponentModel.DataAnnotations;
using GIBS.Module.FBClients.Models;

namespace GIBS.Module.FBClients.Inventory.Models
{
    public class FBSupplier : FBClientsLegacyRecord
    {
        [Key]
        public int SupplierId { get; set; }
        public int? LegacySupplierId { get; set; }
        [MaxLength(50)]
        public string? SupplierName { get; set; }
        public bool GBFB { get; set; }
        [MaxLength(50)]
        public string? Address { get; set; }
        [MaxLength(50)]
        public string? City { get; set; }
        [MaxLength(50)]
        public string? State { get; set; }
        [MaxLength(50)]
        public string? Zip { get; set; }
        [MaxLength(50)]
        public string? SupplierPhone { get; set; }
        [MaxLength(50)]
        public string? Salesman { get; set; }
        [MaxLength(50)]
        public string? SalesmanPhone { get; set; }
        public int ModuleId { get; set; }
        public int? LegacyPortalId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
