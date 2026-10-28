#nullable enable

using System;
using Oqtane.Models;

namespace GIBS.Module.FBClients.Models
{
    public abstract class FBClientsLegacyRecord : ModelBase
    {
        public DateTime? LegacyCreatedOnDate { get; set; }
        public int? LegacyCreatedByUserId { get; set; }
        public DateTime? LegacyLastModifiedOnDate { get; set; }
        public int? LegacyLastModifiedByUserId { get; set; }
    }
}
