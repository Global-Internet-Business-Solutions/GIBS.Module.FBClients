#nullable enable

using System.ComponentModel.DataAnnotations;
using GIBS.Module.FBClients.Models;

namespace GIBS.Module.FBClients.Inventory.Models
{
    public class FBProductCategory : FBClientsLegacyRecord
    {
        [Key]
        public int ProductCategoryId { get; set; }
        public int? LegacyProductCategoryId { get; set; }
        [MaxLength(50)]
        public string? ProductCategory { get; set; }
        public int ModuleId { get; set; }
        public int? LegacyPortalId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SortOrder { get; set; }
        public string? OrderingInstructions { get; set; }
    }
}
