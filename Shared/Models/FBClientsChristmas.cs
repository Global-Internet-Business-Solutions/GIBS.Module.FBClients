#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsChristmas")]
    public class FBClientsChristmas : FBClientsLegacyRecord
    {
        [Key]
        public int xMasId { get; set; }
        public int FBClientsId { get; set; }
        public int? FBClientsAddFamMemId { get; set; }
        public int? LegacyXMasId { get; set; }
        public int? LegacyClAddFamMemId { get; set; }
        public int xMasYear { get; set; }
        [MaxLength(500)]
        public string? Sizes { get; set; }
        public bool? BikeRaffle { get; set; }
        public DateOnly? BikeAwardedDate { get; set; }
        public bool? WantsToys { get; set; }
        public bool? VerifiedToys { get; set; }
        public DateOnly? ReceivedToysDate { get; set; }
        [MaxLength(500)]
        public string? XmasNotes { get; set; }
        [MaxLength(50)]
        public string? XmasGift1 { get; set; }
        [MaxLength(50)]
        public string? XmasGift2 { get; set; }
        [MaxLength(50)]
        public string? XmasGift1Size { get; set; }
        [MaxLength(50)]
        public string? XmasGift2Size { get; set; }
        [MaxLength(40)]
        public string? XmasGiftRecordID { get; set; }
    }
}
