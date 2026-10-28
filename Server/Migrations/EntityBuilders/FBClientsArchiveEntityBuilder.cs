using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public abstract class FBClientsArchiveEntityBuilder<TBuilder> : FBClientsLegacyEntityBuilder<TBuilder>
        where TBuilder : FBClientsLegacyEntityBuilder<TBuilder>
    {
        protected FBClientsArchiveEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database, string tableName)
            : base(migrationBuilder, database)
        {
            EntityTableName = tableName;
        }

        protected void AddArchiveAuditColumns(ColumnsBuilder table)
        {
            AddLegacyAuditColumns(table);
        }

        protected void AddLegacyClientIdColumn(ColumnsBuilder table)
        {
            AddIntegerColumn(table, "LegacyClientId");
        }
    }
}
