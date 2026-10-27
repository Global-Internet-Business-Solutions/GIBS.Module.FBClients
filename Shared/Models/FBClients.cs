using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Models;

namespace GIBS.Module.FBClients.Models
{
    [Table("GIBSFBClients")]
    public class FBClients : ModelBase
    {
        [Key]
        public int FBClientsId { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; }
    }
}
