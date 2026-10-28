using GIBS.Module.FBClients.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oqtane.Databases.Interfaces;

namespace GIBS.Module.FBClients.Repository
{
    public static class FBClientsArchiveConfiguration
    {
        public static void Configure(ModelBuilder builder, IDatabase database)
        {
            var clients = ConfigureArchive<FBClientsMerged>(builder, database, "GIBSFBClientsMerged", isChild: false);
            clients.Property(x => x.Latitude).HasPrecision(10, 5);
            clients.Property(x => x.Longitude).HasPrecision(10, 5);
            clients.HasIndex(x => new { x.ModuleId, x.LegacyMergedWithClientId })
                .HasDatabaseName("IX_GIBSFBClientsMerged_ModuleId_LegacyMergedWithClientId");

            var family = ConfigureArchive<FBClientsAddFamMemMerged>(builder, database, "GIBSFBClientsAddFamMemMerged", isChild: true);
            family.Property(x => x.AFMEthnicity).IsUnicode(false);

            var income = ConfigureArchive<FBClientsIncomeAndExpenseMerged>(builder, database, "GIBSFBClientsIncomeAndExpenseMerged", isChild: true);
            income.Property(x => x.IeAmount).HasPrecision(19, 4);

            ConfigureArchive<FBClientsTrueFalseQuestionsMerged>(builder, database, "GIBSFBClientsTrueFalseQuestionsMerged", isChild: true);
            ConfigureArchive<FBClientsVisitsMerged>(builder, database, "GIBSFBClientsVisitsMerged", isChild: true);

            var christmas = ConfigureArchive<FBClientsChristmasMerged>(builder, database, "GIBSFBClientsChristmasMerged", isChild: true);
            christmas.Property(x => x.Sizes).IsUnicode(false);
        }

        private static EntityTypeBuilder<TEntity> ConfigureArchive<TEntity>(ModelBuilder builder, IDatabase database,
            string tableName, bool isChild) where TEntity : FBClientsLegacyRecord
        {
            var entity = builder.Entity<TEntity>();
            entity.ToTable(database.RewriteName(tableName));
            entity.HasKey("ArchiveId");
            entity.HasOne<Oqtane.Models.Module>().WithMany().HasForeignKey("ModuleId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property<string>("CreatedBy").HasMaxLength(256).IsRequired();
            entity.Property<string>("ModifiedBy").HasMaxLength(256).IsRequired();
            entity.HasIndex("ModuleId", "LegacyRecordId")
                .HasDatabaseName($"IX_{tableName}_ModuleId_LegacyRecordId");

            if (isChild)
            {
                entity.HasIndex("ModuleId", "LegacyClientId")
                    .HasDatabaseName($"IX_{tableName}_ModuleId_LegacyClientId");
            }

            return entity;
        }
    }
}
