using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsVisitItemEntityBuilder : AuditableBaseEntityBuilder<FBClientsVisitItemEntityBuilder>
    {
        public FBClientsVisitItemEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBClientsVisitItem";
            PrimaryKey = new PrimaryKey<FBClientsVisitItemEntityBuilder>("PK_GIBSFBClientsVisitItem", x => x.VisitItemId);
            ForeignKeys.Add(new ForeignKey<FBClientsVisitItemEntityBuilder>("FK_GIBSFBClientsVisitItem_GIBSFBClientsVisits", x => x.VisitId, "GIBSFBClientsVisits", "VisitId", ReferentialAction.Cascade));
        }
        protected override FBClientsVisitItemEntityBuilder BuildTable(ColumnsBuilder table)
        {
            VisitItemId = AddAutoIncrementColumn(table, "VisitItemId");
            VisitId = AddIntegerColumn(table, "VisitId");
            AddIntegerColumn(table, "LegacyVisitItemId", nullable: true);
            AddIntegerColumn(table, "ProductId");
            AddIntegerColumn(table, "Quantity");
            AddAuditableColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> VisitItemId { get; set; }
        public OperationBuilder<AddColumnOperation> VisitId { get; set; }
    }
}
