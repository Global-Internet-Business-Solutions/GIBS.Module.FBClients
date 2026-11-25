using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.Fulfillment
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new()
        {
            Name = "FBClients Fulfillment",
            Description = "Enter paper orders, pack food orders, and notify clients when they are ready for pickup",
            Version = "1.0.2",
            ReleaseVersions = "1.0.2",
            Dependencies = "GIBS.Module.FBClients.Shared.Oqtane",
            PackageName = "GIBS.Module.FBClients"
        };
    }
}
