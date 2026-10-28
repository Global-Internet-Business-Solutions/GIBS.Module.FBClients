#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Models;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsAddFamMem")]
    public class FBClientsAddFamMem : ModelBase
    {
        [Key]
        public int FBClientsAddFamMemId { get; set; }
        public int FBClientsId { get; set; }
        public int? LegacyClAddFamMemId { get; set; }
        public DateTime? LegacyCreatedOnDate { get; set; }
        public int? LegacyCreatedByUserId { get; set; }
        public DateTime? LegacyLastModifiedOnDate { get; set; }
        public int? LegacyLastModifiedByUserId { get; set; }

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
