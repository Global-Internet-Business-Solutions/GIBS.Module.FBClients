#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsChristmasMerged")]
    public class FBClientsChristmasMerged : FBClientsLegacyRecord
    {
        [Key]
        public int ArchiveId { get; set; }
        public int ModuleId { get; set; }
        public int LegacyRecordId { get; set; }
        public int LegacyClientId { get; set; }
        public int LegacyClAddFamMemId { get; set; }
        public int xMasYear { get; set; }

        [MaxLength(500)]
        public string? Sizes { get; set; }
        public bool? BikeRaffle { get; set; }
        public DateOnly? BikeAwardedDate { get; set; }
        public bool? WantsToys { get; set; }
        public bool? VerifiedToys { get; set; }
        public DateOnly? ReceivedToysDate { get; set; }
    }
}
