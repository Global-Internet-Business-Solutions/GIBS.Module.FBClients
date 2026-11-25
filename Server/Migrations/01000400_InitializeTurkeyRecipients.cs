using System;
using GIBS.Module.FBClients.Migrations.EntityBuilders;
using GIBS.Module.FBClients.Repository;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations
{
    [DbContext(typeof(FBClientsContext))]
    [Migration("GIBS.Module.FBClients.01.00.04.00")]
    public class InitializeTurkeyRecipients(IDatabase database) : MultiDatabaseMigration(database)
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var recipients = new FBTurkeyRecipientEntityBuilder(migrationBuilder, ActiveDatabase);
            recipients.Create();
            recipients.AddIndex("IX_GIBSFBTurkeyRecipients_FBClientsId_Year", new[] { "FBClientsId", "Year" });
            recipients.AddIndex("IX_GIBSFBTurkeyRecipients_FBClientsId_Year_Holiday", new[] { "FBClientsId", "Year", "Holiday" });
            var checkpoint = ActiveDatabase.RewriteName("GIBSFBClientsImportCheckpoint");
            var primaryKey = ActiveDatabase.RewriteName("PK_GIBSFBClientsImportCheckpoint");
            migrationBuilder.DropPrimaryKey(name: primaryKey, table: checkpoint);
            migrationBuilder.AddColumn<string>(name: ActiveDatabase.RewriteName("RunKey"), table: checkpoint,
                maxLength: 128, nullable: false, defaultValue: "1main");
            migrationBuilder.AddPrimaryKey(name: primaryKey, table: checkpoint,
                columns: new[] { ActiveDatabase.RewriteName("DestinationModuleId"), ActiveDatabase.RewriteName("RunKey") });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Downgrading would discard supplementary import checkpoints. Review and archive import progress before a manual recovery.");
        }
    }
}
