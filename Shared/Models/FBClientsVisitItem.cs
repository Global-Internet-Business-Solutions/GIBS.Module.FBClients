#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Models;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsVisitItem")]
    public class FBClientsVisitItem : ModelBase
    {
        [Key]
        public int VisitItemId { get; set; }
        public int VisitId { get; set; }
        public int? LegacyVisitItemId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
