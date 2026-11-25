#nullable enable

using System.ComponentModel.DataAnnotations;
using GIBS.Module.FBClients.Models;

namespace GIBS.Module.FBClients.Inventory.Models
{
    public class FBProduct : FBClientsLegacyRecord
    {
        [Key]
        public int ProductId { get; set; }
        public int? LegacyProductId { get; set; }
        [MaxLength(100)]
        public string? ProductName { get; set; }
        public decimal? CasePrice { get; set; } = 0;
        public int? CaseCount { get; set; } = 0;
        public int? ProductCategoryId { get; set; }
        public int ModuleId { get; set; }
        public int? LegacyPortalId { get; set; }
        public decimal? CaseWeight { get; set; }
        public bool IsActive { get; set; } = true;
        public int? Limit { get; set; } = 1;
        [MaxLength(50)]
        public string? LimitQuantities { get; set; }
    }
}
