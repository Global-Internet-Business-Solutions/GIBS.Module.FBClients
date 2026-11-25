using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.ImportData
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new()
        {
            Name = "FBClients Import Data",
            Description = "Prepare a read-only DNN client history import plan",
            Version = "1.0.2",
            ReleaseVersions = "1.0.2",
            Dependencies = "GIBS.Module.FBClients.Shared.Oqtane",
            PackageName = "GIBS.Module.FBClients"
        };
    }
}
