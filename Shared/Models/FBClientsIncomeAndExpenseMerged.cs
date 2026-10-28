#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsIncomeAndExpenseMerged")]
    public class FBClientsIncomeAndExpenseMerged : FBClientsLegacyRecord
    {
        [Key]
        public int ArchiveId { get; set; }
        public int ModuleId { get; set; }
        public int LegacyRecordId { get; set; }
        public int LegacyClientId { get; set; }

        [MaxLength(2)]
        public string? IeType { get; set; }
        [MaxLength(100)]
        public string? IeDescription { get; set; }
        public decimal? IeAmount { get; set; }
    }
}
