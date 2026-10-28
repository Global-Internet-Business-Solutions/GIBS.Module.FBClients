#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsVisitsMerged")]
    public class FBClientsVisitsMerged : FBClientsLegacyRecord
    {
        [Key]
        public int ArchiveId { get; set; }
        public int ModuleId { get; set; }
        public int LegacyRecordId { get; set; }
        public int LegacyClientId { get; set; }

        public DateOnly VisitDate { get; set; }
        public string? VisitNotes { get; set; }
        public int? VisitNumBags { get; set; }
    }
}
