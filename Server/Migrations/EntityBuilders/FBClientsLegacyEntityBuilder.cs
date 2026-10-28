using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public abstract class FBClientsLegacyEntityBuilder<T> : AuditableBaseEntityBuilder<T>
        where T : FBClientsLegacyEntityBuilder<T>
    {
        protected FBClientsLegacyEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database)
            : base(migrationBuilder, database)
        {
        }

        protected void AddLegacyAuditColumns(ColumnsBuilder table)
        {
            AddDateTimeColumn(table, "LegacyCreatedOnDate", nullable: true);
            AddIntegerColumn(table, "LegacyCreatedByUserId", nullable: true);
            AddDateTimeColumn(table, "LegacyLastModifiedOnDate", nullable: true);
            AddIntegerColumn(table, "LegacyLastModifiedByUserId", nullable: true);
            AddAuditableColumns(table);
        }
    }
}