#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClientsIDPhoto")]
    public class FBClientsIDPhoto : FBClientsLegacyRecord
    {
        [Key]
        public int ImageId { get; set; }
        public int FBClientsId { get; set; }
        public int? LegacyImageId { get; set; }
        [Required]
        public byte[] IDPhoto { get; set; } = System.Array.Empty<byte>();
    }
}
