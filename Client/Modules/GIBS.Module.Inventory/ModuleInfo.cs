using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.Inventory
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new ModuleDefinition
        {
            Name = "FBClients Inventory",
            Description = "Manage the food bank catalog, suppliers, invoices, and inventory reports",
            Version = "1.0.2",
            ReleaseVersions = "1.0.2",
            Dependencies = "GIBS.Module.FBClients.Shared.Oqtane",
            PackageName = "GIBS.Module.FBClients"
        };
    }
}
