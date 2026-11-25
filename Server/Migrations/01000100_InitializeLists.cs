using GIBS.Module.FBClients.Lists.Migrations.EntityBuilders;
using GIBS.Module.FBClients.Repository;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations
{
    [DbContext(typeof(FBClientsContext))]
    [Migration("GIBS.Module.FBClients.01.00.01.00")]
    public class InitializeLists : MultiDatabaseMigration
    {
        public InitializeLists(IDatabase database) : base(database)
        {
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            new ListsEntityBuilder(migrationBuilder, ActiveDatabase).Create();
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            new ListsEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
        }
    }
}
