#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsVisits")]
    public class FBClientsVisits : FBClientsLegacyRecord
    {
        [Key]
        public int VisitId { get; set; }
        public int FBClientsId { get; set; }
        public int? LegacyVisitId { get; set; }
        public DateOnly VisitDate { get; set; }
        [MaxLength(800)]
        public string? VisitNotes { get; set; }
        public int? VisitNumBags { get; set; }
        [Required(AllowEmptyStrings = true), MaxLength(50)]
        public string ServiceLocation { get; set; } = string.Empty;
        public byte[]? ClientSignature { get; set; }
        public int? OrderStatusCode { get; set; } = 0;
    }
}
