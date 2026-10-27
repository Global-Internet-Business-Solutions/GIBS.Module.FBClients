using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Oqtane.Modules;
using Oqtane.Repository;
using Oqtane.Infrastructure;
using Oqtane.Repository.Databases.Interfaces;

namespace GIBS.Module.FBClients.Repository
{
    public class FBClientsContext : DBContextBase, ITransientService, IMultiDatabase
    {
        public virtual DbSet<Models.FBClients> FBClients { get; set; }

        public FBClientsContext(IDBContextDependencies DBContextDependencies) : base(DBContextDependencies)
        {
            // ContextBase handles multi-tenant database connections
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Models.FBClients>().ToTable(ActiveDatabase.RewriteName("GIBSFBClients"));
        }
    }
}
