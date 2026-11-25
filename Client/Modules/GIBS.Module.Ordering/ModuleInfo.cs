using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.Ordering
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new ModuleDefinition
        {
            Name = "FBClients Ordering",
            Description = "Ordering for Food Bank Clients",
            Version = "1.0.2",
            ReleaseVersions = "1.0.1,1.0.2",
            Dependencies = "GIBS.Module.FBClients.Shared.Oqtane",
            PackageName = "GIBS.Module.FBClients"
        };
    }
}
