using Microsoft.EntityFrameworkCore;
using Oqtane.Databases.Interfaces;
using GIBS.Module.FBClients.Models;

namespace GIBS.Module.FBClients.Repository
{
    public static class FBClientsActivityConfiguration
    {
        public static void Configure(ModelBuilder builder, IDatabase database)
        {
            var visits = builder.Entity<FBClientsVisits>();
            visits.ToTable(database.RewriteName("GIBSFBClientsVisits"));
            visits.HasOne<Models.FBClients>().WithMany().HasForeignKey(x => x.FBClientsId).OnDelete(DeleteBehavior.Cascade);
            visits.Property(x => x.ServiceLocation).IsUnicode(false).HasDefaultValue("").ValueGeneratedNever();
            visits.Property(x => x.OrderStatusCode).HasDefaultValue(0).ValueGeneratedNever();
            visits.HasIndex(x => new { x.FBClientsId, x.VisitDate });
            visits.HasIndex(x => new { x.FBClientsId, x.LegacyVisitId });

            var items = builder.Entity<FBClientsVisitItem>();
            items.ToTable(database.RewriteName("GIBSFBClientsVisitItem"));
            items.HasOne<FBClientsVisits>().WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
            items.HasIndex(x => new { x.VisitId, x.LegacyVisitItemId });

            var income = builder.Entity<FBClientsIncomeAndExpense>();
            income.ToTable(database.RewriteName("GIBSFBClientsIncomeAndExpense"));
            income.HasOne<Models.FBClients>().WithMany().HasForeignKey(x => x.FBClientsId).OnDelete(DeleteBehavior.Cascade);
            income.Property(x => x.IeAmount).HasPrecision(19, 4);
            income.HasIndex(x => new { x.FBClientsId, x.LegacyIeId });

            var questions = builder.Entity<FBClientsTrueFalseQuestions>();
            questions.ToTable(database.RewriteName("GIBSFBClientsTrueFalseQuestions"));
            questions.HasOne<Models.FBClients>().WithMany().HasForeignKey(x => x.FBClientsId).OnDelete(DeleteBehavior.Cascade);
            questions.Property(x => x.TfAnswer).HasDefaultValue(false);
            questions.HasIndex(x => new { x.FBClientsId, x.LegacyTfId });

            var photos = builder.Entity<FBClientsIDPhoto>();
            photos.ToTable(database.RewriteName("GIBSFBClientsIDPhoto"));
            photos.HasOne<Models.FBClients>().WithMany().HasForeignKey(x => x.FBClientsId).OnDelete(DeleteBehavior.Cascade);
            photos.HasIndex(x => new { x.FBClientsId, x.LegacyImageId });

            var christmas = builder.Entity<FBClientsChristmas>();
            christmas.ToTable(database.RewriteName("GIBSFBClientsChristmas"));
            christmas.HasOne<Models.FBClients>().WithMany().HasForeignKey(x => x.FBClientsId).OnDelete(DeleteBehavior.Cascade);
            christmas.HasOne<FBClientsAddFamMem>().WithMany()
                .HasForeignKey(x => new { x.FBClientsAddFamMemId, x.FBClientsId })
                .HasPrincipalKey(x => new { x.FBClientsAddFamMemId, x.FBClientsId }).OnDelete(DeleteBehavior.NoAction);
            christmas.HasIndex(x => new { x.FBClientsId, x.xMasYear });
            christmas.HasIndex(x => new { x.FBClientsId, x.LegacyXMasId });
            christmas.HasIndex(x => new { x.FBClientsAddFamMemId, x.FBClientsId });
            christmas.Property(x => x.Sizes).IsUnicode(false);
            christmas.Property(x => x.XmasNotes).IsUnicode(false);
            christmas.Property(x => x.XmasGift1).IsUnicode(false);
            christmas.Property(x => x.XmasGift2).IsUnicode(false);
            christmas.Property(x => x.XmasGift1Size).IsUnicode(false);
            christmas.Property(x => x.XmasGift2Size).IsUnicode(false);
            christmas.Property(x => x.XmasGiftRecordID).IsUnicode(false);
        }
    }
}
