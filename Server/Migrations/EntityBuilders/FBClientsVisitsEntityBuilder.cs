using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsVisitsEntityBuilder : FBClientsLegacyEntityBuilder<FBClientsVisitsEntityBuilder>
    {
        public FBClientsVisitsEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBClientsVisits";
            PrimaryKey = new PrimaryKey<FBClientsVisitsEntityBuilder>("PK_GIBSFBClientsVisits", x => x.VisitId);
            ForeignKeys.Add(new ForeignKey<FBClientsVisitsEntityBuilder>("FK_GIBSFBClientsVisits_GIBSFBClients", x => x.FBClientsId, "GIBSFBClients", "FBClientsId", ReferentialAction.Cascade));
        }
        protected override FBClientsVisitsEntityBuilder BuildTable(ColumnsBuilder table)
        {
            VisitId = AddAutoIncrementColumn(table, "VisitId");
            FBClientsId = AddIntegerColumn(table, "FBClientsId");
            AddIntegerColumn(table, "LegacyVisitId", nullable: true);
            AddDateOnlyColumn(table, "VisitDate");
            AddStringColumn(table, "VisitNotes", 800, nullable: true);
            AddIntegerColumn(table, "VisitNumBags", nullable: true);
            AddStringColumn(table, "ServiceLocation", 50, nullable: false, unicode: false, defaultValue: "");
            table.Column<byte[]>(name: ActiveDatabase.RewriteName("ClientSignature"), nullable: true);
            AddIntegerColumn(table, "OrderStatusCode", nullable: true, defaultValue: 0);
            AddLegacyAuditColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> VisitId { get; set; }
        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
    }
}
