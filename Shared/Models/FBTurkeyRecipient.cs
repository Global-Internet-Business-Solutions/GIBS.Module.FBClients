#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Models;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBTurkeyRecipients")]
    public class FBTurkeyRecipient : ModelBase
    {
        [Key]
        public int TurkeyId { get; set; }
        public int FBClientsId { get; set; }
        public int Year { get; set; }
        [MaxLength(50)]
        public string? Holiday { get; set; }
        public DateTime? LegacyCreatedOnDate { get; set; }
    }
}
