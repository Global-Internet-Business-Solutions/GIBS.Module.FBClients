using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Models;

namespace GIBS.Module.FBClients.Lists.Models
{
    [Table("GIBSFBClientsLists")]
    public class Lists : ModelBase
    {
        [Key]
        public int ListsId { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; }
        [Required, MaxLength(100)]
        public string ListName { get; set; } = string.Empty;
        [Required, MaxLength(300)]
        public string Value { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Text { get; set; } = string.Empty;
        public int ParentID { get; set; }
        public int Level { get; set; }
        public int SortOrder { get; set; }
        public int DefinitionID { get; set; }
        [MaxLength(500)]
        public string Description { get; set; }
        public int SiteID { get; set; }
        public bool SystemList { get; set; }
    }
}
