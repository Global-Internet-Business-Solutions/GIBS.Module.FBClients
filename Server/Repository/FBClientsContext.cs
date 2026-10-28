using Microsoft.EntityFrameworkCore;
using System.Linq;
using Oqtane.Modules;
using Oqtane.Repository;
using Oqtane.Infrastructure;
using Oqtane.Repository.Databases.Interfaces;

namespace GIBS.Module.FBClients.Repository
{
    public class FBClientsContext : DBContextBase, ITransientService, IMultiDatabase
    {
        public virtual DbSet<Models.FBClients> FBClients { get; set; }
        public virtual DbSet<Models.FBClientsAddFamMem> FBClientsAddFamMem { get; set; }
        public virtual DbSet<Models.FBClientsVisits> FBClientsVisits { get; set; }
        public virtual DbSet<Models.FBClientsVisitItem> FBClientsVisitItems { get; set; }
        public virtual DbSet<Models.FBClientsIncomeAndExpense> FBClientsIncomeAndExpenses { get; set; }
        public virtual DbSet<Models.FBClientsTrueFalseQuestions> FBClientsTrueFalseQuestions { get; set; }
        public virtual DbSet<Models.FBClientsIDPhoto> FBClientsIDPhotos { get; set; }
        public virtual DbSet<Models.FBClientsChristmas> FBClientsChristmas { get; set; }
        public virtual DbSet<Models.FBClientsMerged> FBClientsMerged { get; set; }
        public virtual DbSet<Models.FBClientsAddFamMemMerged> FBClientsAddFamMemMerged { get; set; }
        public virtual DbSet<Models.FBClientsIncomeAndExpenseMerged> FBClientsIncomeAndExpenseMerged { get; set; }
        public virtual DbSet<Models.FBClientsTrueFalseQuestionsMerged> FBClientsTrueFalseQuestionsMerged { get; set; }
        public virtual DbSet<Models.FBClientsVisitsMerged> FBClientsVisitsMerged { get; set; }
        public virtual DbSet<Models.FBClientsChristmasMerged> FBClientsChristmasMerged { get; set; }

        public FBClientsContext(IDBContextDependencies DBContextDependencies) : base(DBContextDependencies)
        {
            // ContextBase handles multi-tenant database connections
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Oqtane.Models.Module>()
                .ToTable(ActiveDatabase.RewriteName("Module"), table => table.ExcludeFromMigrations());

            var clients = builder.Entity<Models.FBClients>();
            clients.ToTable(ActiveDatabase.RewriteName("GIBSFBClients"));
            clients.HasOne<Oqtane.Models.Module>().WithMany().HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
            clients.Property(x => x.Latitude).HasPrecision(10, 5);
            clients.Property(x => x.Longitude).HasPrecision(10, 5);
            clients.Property(x => x.Disability).IsUnicode(false);
            clients.Property(x => x.ServiceLocation).IsUnicode(false);
            clients.Property(x => x.ClientLanguage).IsUnicode(false);
            clients.Property(x => x.ClientDOBVerify).HasDefaultValue(false);
            clients.Property(x => x.ClientAddressVerify).HasDefaultValue(false);
            clients.Property(x => x.SubjectToReview).HasDefaultValue(false);
            clients.Property(x => x.OneBagOnly).HasDefaultValue(false);
            clients.Property(x => x.IsActive).HasDefaultValue(true).ValueGeneratedNever();
            clients.Property(x => x.IsLocked).HasDefaultValue(false).ValueGeneratedNever();
            clients.Property(x => x.PrimaryIncomeSource).HasDefaultValue("").ValueGeneratedNever();
            clients.HasIndex(x => new { x.ModuleId, x.ClientLastName, x.ClientFirstName });
            clients.HasIndex(x => new { x.ModuleId, x.LegacyClientId });

            var family = builder.Entity<Models.FBClientsAddFamMem>();
            family.ToTable(ActiveDatabase.RewriteName("GIBSFBClientsAddFamMem"));
            family.HasOne<Models.FBClients>().WithMany().HasForeignKey(x => x.FBClientsId)
                .OnDelete(DeleteBehavior.Cascade);
            family.Property(x => x.AFMEthnicity).IsUnicode(false);
            family.Property(x => x.AFMDOBVerify).HasDefaultValue(false);
            family.HasIndex(x => x.FBClientsId);
            family.HasIndex(x => new { x.FBClientsId, x.LegacyClAddFamMemId });

            FBClientsActivityConfiguration.Configure(builder, ActiveDatabase);
            FBClientsArchiveConfiguration.Configure(builder, ActiveDatabase);

            foreach (var entityType in builder.Model.GetEntityTypes()
                .Where(x => x.ClrType.Namespace == typeof(Models.FBClients).Namespace).ToList())
            {
                builder.Entity(entityType.ClrType).Property<string>("CreatedBy").HasMaxLength(256).IsRequired();
                builder.Entity(entityType.ClrType).Property<string>("ModifiedBy").HasMaxLength(256).IsRequired();
            }
        }
    }
}
