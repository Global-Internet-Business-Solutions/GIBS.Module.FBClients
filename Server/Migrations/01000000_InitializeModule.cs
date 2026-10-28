using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using GIBS.Module.FBClients.Migrations.EntityBuilders;
using GIBS.Module.FBClients.Repository;

namespace GIBS.Module.FBClients.Migrations
{
    [DbContext(typeof(FBClientsContext))]
    [Migration("GIBS.Module.FBClients.01.00.00.00")]
    public class InitializeModule : MultiDatabaseMigration
    {
        public InitializeModule(IDatabase database) : base(database)
        {
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var entityBuilder = new FBClientsEntityBuilder(migrationBuilder, ActiveDatabase);
            entityBuilder.Create();
            entityBuilder.AddIndex("IX_GIBSFBClients_ModuleId_ClientLastName_ClientFirstName",
                new[] { "ModuleId", "ClientLastName", "ClientFirstName" });
            entityBuilder.AddIndex("IX_GIBSFBClients_ModuleId_LegacyClientId",
                new[] { "ModuleId", "LegacyClientId" });

            var familyBuilder = new FBClientsAddFamMemEntityBuilder(migrationBuilder, ActiveDatabase);
            familyBuilder.Create();
            familyBuilder.AddIndex("IX_GIBSFBClientsAddFamMem_FBClientsId", "FBClientsId");
            familyBuilder.AddIndex("IX_GIBSFBClientsAddFamMem_FBClientsId_LegacyClAddFamMemId",
                new[] { "FBClientsId", "LegacyClAddFamMemId" });
            migrationBuilder.AddUniqueConstraint(ActiveDatabase.RewriteName("AK_GIBSFBClientsAddFamMem_Id_Client"),
                ActiveDatabase.RewriteName("GIBSFBClientsAddFamMem"),
                new[] { ActiveDatabase.RewriteName("FBClientsAddFamMemId"), ActiveDatabase.RewriteName("FBClientsId") });

            var visits = new FBClientsVisitsEntityBuilder(migrationBuilder, ActiveDatabase);
            visits.Create();
            visits.AddIndex("IX_GIBSFBClientsVisits_FBClientsId_VisitDate", new[] { "FBClientsId", "VisitDate" });
            visits.AddIndex("IX_GIBSFBClientsVisits_FBClientsId_LegacyVisitId", new[] { "FBClientsId", "LegacyVisitId" });
            var items = new FBClientsVisitItemEntityBuilder(migrationBuilder, ActiveDatabase);
            items.Create();
            items.AddIndex("IX_GIBSFBClientsVisitItem_VisitId_LegacyVisitItemId", new[] { "VisitId", "LegacyVisitItemId" });
            var income = new FBClientsIncomeAndExpenseEntityBuilder(migrationBuilder, ActiveDatabase);
            income.Create();
            income.AddIndex("IX_GIBSFBClientsIncomeAndExpense_FBClientsId_LegacyIeId", new[] { "FBClientsId", "LegacyIeId" });
            var questions = new FBClientsTrueFalseQuestionsEntityBuilder(migrationBuilder, ActiveDatabase);
            questions.Create();
            questions.AddIndex("IX_GIBSFBClientsTrueFalseQuestions_FBClientsId_LegacyTfId", new[] { "FBClientsId", "LegacyTfId" });
            var photos = new FBClientsIDPhotoEntityBuilder(migrationBuilder, ActiveDatabase);
            photos.Create();
            photos.AddIndex("IX_GIBSFBClientsIDPhoto_FBClientsId_LegacyImageId", new[] { "FBClientsId", "LegacyImageId" });
            var christmas = new FBClientsChristmasEntityBuilder(migrationBuilder, ActiveDatabase);
            christmas.Create();
            christmas.AddIndex("IX_GIBSFBClientsChristmas_FBClientsId_xMasYear", new[] { "FBClientsId", "xMasYear" });
            christmas.AddIndex("IX_GIBSFBClientsChristmas_FBClientsId_LegacyXMasId", new[] { "FBClientsId", "LegacyXMasId" });
            christmas.AddIndex("IX_GIBSFBClientsChristmas_Family_Client", new[] { "FBClientsAddFamMemId", "FBClientsId" });
            migrationBuilder.AddForeignKey(ActiveDatabase.RewriteName("FK_GIBSFBClientsChristmas_Family_Client"),
                ActiveDatabase.RewriteName("GIBSFBClientsChristmas"),
                new[] { ActiveDatabase.RewriteName("FBClientsAddFamMemId"), ActiveDatabase.RewriteName("FBClientsId") },
                ActiveDatabase.RewriteName("GIBSFBClientsAddFamMem"),
                principalColumns: new[] { ActiveDatabase.RewriteName("FBClientsAddFamMemId"), ActiveDatabase.RewriteName("FBClientsId") },
                onDelete: ReferentialAction.NoAction);

            CreateArchives(migrationBuilder); 
        }

        protected override void Down(MigrationBuilder migrationBuilder) 
        {
            new FBClientsChristmasMergedEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsVisitsMergedEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsTrueFalseQuestionsMergedEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsIncomeAndExpenseMergedEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsAddFamMemMergedEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsMergedEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsChristmasEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsIDPhotoEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsTrueFalseQuestionsEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsIncomeAndExpenseEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsVisitItemEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBClientsVisitsEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            var familyBuilder = new FBClientsAddFamMemEntityBuilder(migrationBuilder, ActiveDatabase);
            familyBuilder.Drop();

            var entityBuilder = new FBClientsEntityBuilder(migrationBuilder, ActiveDatabase);
            entityBuilder.Drop();
        }

        private void CreateArchives(MigrationBuilder migrationBuilder)
        {
            CreateArchive(new FBClientsMergedEntityBuilder(migrationBuilder, ActiveDatabase), "GIBSFBClientsMerged", "LegacyMergedWithClientId");
            CreateArchive(new FBClientsAddFamMemMergedEntityBuilder(migrationBuilder, ActiveDatabase), "GIBSFBClientsAddFamMemMerged", "LegacyClientId");
            CreateArchive(new FBClientsIncomeAndExpenseMergedEntityBuilder(migrationBuilder, ActiveDatabase), "GIBSFBClientsIncomeAndExpenseMerged", "LegacyClientId");
            CreateArchive(new FBClientsTrueFalseQuestionsMergedEntityBuilder(migrationBuilder, ActiveDatabase), "GIBSFBClientsTrueFalseQuestionsMerged", "LegacyClientId");
            CreateArchive(new FBClientsVisitsMergedEntityBuilder(migrationBuilder, ActiveDatabase), "GIBSFBClientsVisitsMerged", "LegacyClientId");
            CreateArchive(new FBClientsChristmasMergedEntityBuilder(migrationBuilder, ActiveDatabase), "GIBSFBClientsChristmasMerged", "LegacyClientId");
        }

        private static void CreateArchive<T>(Oqtane.Migrations.EntityBuilders.BaseEntityBuilder<T> builder,
            string tableName, string clientColumn) where T : Oqtane.Migrations.EntityBuilders.BaseEntityBuilder<T>
        {
            builder.Create();
            builder.AddIndex("IX_" + tableName + "_ModuleId_LegacyRecordId", new[] { "ModuleId", "LegacyRecordId" });
            builder.AddIndex("IX_" + tableName + "_ModuleId_" + clientColumn, new[] { "ModuleId", clientColumn });
        }
    }
}
