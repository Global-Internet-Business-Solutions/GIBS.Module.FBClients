using GIBS.Module.FBClients.Repository;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations
{
    [DbContext(typeof(FBClientsContext))]
    [Migration("GIBS.Module.FBClients.01.00.03.00")]
    public class InitializeImportCheckpoints(IDatabase database) : MultiDatabaseMigration(database)
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: ActiveDatabase.RewriteName("GIBSFBClientsImportCheckpoint"),
                columns: table => new
                {
                    DestinationModuleId = table.Column<int>(name: ActiveDatabase.RewriteName("DestinationModuleId"), nullable: false),
                    ImportModuleId = table.Column<int>(name: ActiveDatabase.RewriteName("ImportModuleId"), nullable: false),
                    RunIdentity = table.Column<string>(name: ActiveDatabase.RewriteName("RunIdentity"), maxLength: 64, nullable: false),
                    TableIndex = table.Column<int>(name: ActiveDatabase.RewriteName("TableIndex"), nullable: false),
                    LastKey = table.Column<int>(name: ActiveDatabase.RewriteName("LastKey"), nullable: false),
                    ImportedRows = table.Column<long>(name: ActiveDatabase.RewriteName("ImportedRows"), nullable: false),
                    Completed = table.Column<bool>(name: ActiveDatabase.RewriteName("Completed"), nullable: false)
                },
                constraints: table => table.PrimaryKey(ActiveDatabase.RewriteName("PK_GIBSFBClientsImportCheckpoint"), x => x.DestinationModuleId));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(ActiveDatabase.RewriteName("GIBSFBClientsImportCheckpoint"));
        }
    }
}
