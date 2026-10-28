#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsTrueFalseQuestionsMerged")]
    public class FBClientsTrueFalseQuestionsMerged : FBClientsLegacyRecord
    {
        [Key]
        public int ArchiveId { get; set; }
        public int ModuleId { get; set; }
        public int LegacyRecordId { get; set; }
        public int LegacyClientId { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(30)]
        public string TfQuestion { get; set; } = string.Empty;
        public bool TfAnswer { get; set; }
    }
}
