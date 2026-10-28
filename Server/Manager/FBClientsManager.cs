using Oqtane.Modules;
using Oqtane.Models;
using Oqtane.Infrastructure;
using Oqtane.Enums;
using Oqtane.Repository;
using GIBS.Module.FBClients.Repository;

namespace GIBS.Module.FBClients.Manager
{
    public class FBClientsManager : MigratableModuleBase, IInstallable
    {
        private readonly IDBContextDependencies _DBContextDependencies;

        public FBClientsManager(IDBContextDependencies DBContextDependencies)
        {
            _DBContextDependencies = DBContextDependencies;
        }

        public bool Install(Tenant tenant, string version)
        {
            return Migrate(new FBClientsContext(_DBContextDependencies), tenant, MigrationType.Up);
        }

        public bool Uninstall(Tenant tenant)
        {
            return Migrate(new FBClientsContext(_DBContextDependencies), tenant, MigrationType.Down);
        }
    }
}
