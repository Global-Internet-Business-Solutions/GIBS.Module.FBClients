#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsMerged")]
    public class FBClientsMerged : FBClientsLegacyRecord
    {
        [Key]
        public int ArchiveId { get; set; }
        public int ModuleId { get; set; }
        public int LegacyRecordId { get; set; }
        public int LegacyModuleId { get; set; }
        public int? LegacyPortalId { get; set; }
        public int? LegacyCaseWorkerUserId { get; set; }
        public int? LegacyMergedWithClientId { get; set; }

        [MaxLength(30)]
        public string? ClientFirstName { get; set; }
        [MaxLength(15)]
        public string? ClientMiddleInitial { get; set; }
        [MaxLength(30)]
        public string? ClientLastName { get; set; }
        public DateOnly? ClientDOB { get; set; }
        [MaxLength(50)]
        public string? ClientAddress { get; set; }
        [MaxLength(50)]
        public string? ClientCity { get; set; }
        [MaxLength(20)]
        public string? ClientState { get; set; }
        [MaxLength(10)]
        public string? ClientZipCode { get; set; }
        [MaxLength(50)]
        public string? ClientEmailAddress { get; set; }
        [MaxLength(50)]
        public string? ClientIdCard { get; set; }
        [MaxLength(25)]
        public string? ClientPhone { get; set; }
        [MaxLength(25)]
        public string? ClientPhoneType { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public byte[]? ClientPhoto { get; set; }
        [MaxLength(10)]
        public string? ClientSuffix { get; set; }
        public bool ClientDOBVerify { get; set; }
        [MaxLength(100)]
        public string? ClientEthnicity { get; set; }
        [MaxLength(1000)]
        public string? ClientNote { get; set; }
        [MaxLength(20)]
        public string? ClientUnit { get; set; }
        [MaxLength(20)]
        public string? ClientTown { get; set; }
        [MaxLength(10)]
        public string? ClientGender { get; set; }
        [MaxLength(25)]
        public string? ClientType { get; set; }
        public DateOnly? ClientVerifyDate { get; set; }
        public bool ClientAddressVerify { get; set; }
        public DateOnly? ClientAddressVerifyDate { get; set; }
        public bool SubjectToReview { get; set; }
    }
}
