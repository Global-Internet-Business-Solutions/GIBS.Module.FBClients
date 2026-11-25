using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.Lists
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new ModuleDefinition
        {
            Name = "FBClients Lists",
            Description = "Manage reusable lists for Food Bank Clients",
            Version = "1.0.1",
            ServerManagerType = "GIBS.Module.FBClients.Lists.Manager.ListsManager, GIBS.Module.FBClients.Server.Oqtane",
            ReleaseVersions = "1.0.1",
            Dependencies = "GIBS.Module.FBClients.Shared.Oqtane",
            PackageName = "GIBS.Module.FBClients"
        };
    }
}
