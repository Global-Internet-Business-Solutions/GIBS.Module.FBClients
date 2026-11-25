using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.FBClients
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new ModuleDefinition
        {
            Name = "FBClients",
            Description = "Food Bank Clients Manager",
            Version = "1.0.4",
            ServerManagerType = "GIBS.Module.FBClients.Manager.FBClientsManager, GIBS.Module.FBClients.Server.Oqtane",
            ReleaseVersions = "1.0.0,1.0.1,1.0.2,1.0.3,1.0.4",
            Dependencies = "GIBS.Module.FBClients.Shared.Oqtane",
            PackageName = "GIBS.Module.FBClients"
        };
    }
}
