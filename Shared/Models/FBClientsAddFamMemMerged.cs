#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsAddFamMemMerged")]
    public class FBClientsAddFamMemMerged : FBClientsLegacyRecord
    {
        [Key]
        public int ArchiveId { get; set; }
        public int ModuleId { get; set; }
        public int LegacyRecordId { get; set; }
        public int LegacyClientId { get; set; }

        [MaxLength(50)]
        public string? ClAddFamMemFirstName { get; set; }
        [MaxLength(50)]
        public string? ClAddFamMemLastName { get; set; }
        public DateOnly? ClAddFamMemDOB { get; set; }
        [MaxLength(25)]
        public string? AFMRelationship { get; set; }
        [MaxLength(15)]
        public string? AFMMiddleInitial { get; set; }
        [MaxLength(15)]
        public string? AFMSuffix { get; set; }
        public bool AFMDOBVerify { get; set; }
        [MaxLength(100)]
        public string? AFMEthnicity { get; set; }
        [MaxLength(10)]
        public string? AFMGender { get; set; }
    }
}
