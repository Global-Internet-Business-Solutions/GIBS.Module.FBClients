#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsIncomeAndExpense")]
    public class FBClientsIncomeAndExpense : FBClientsLegacyRecord
    {
        [Key]
        public int IeId { get; set; }
        public int FBClientsId { get; set; }
        public int? LegacyIeId { get; set; }
        [MaxLength(2)]
        public string? IeType { get; set; }
        [MaxLength(100)]
        public string? IeDescription { get; set; }
        public decimal? IeAmount { get; set; }
    }
}
