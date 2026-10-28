#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsTrueFalseQuestions")]
    public class FBClientsTrueFalseQuestions : FBClientsLegacyRecord
    {
        [Key]
        public int TfId { get; set; }
        public int FBClientsId { get; set; }
        public int? LegacyTfId { get; set; }
        [Required(AllowEmptyStrings = true), MaxLength(30)]
        public string TfQuestion { get; set; } = string.Empty;
        public bool TfAnswer { get; set; }
    }
}
