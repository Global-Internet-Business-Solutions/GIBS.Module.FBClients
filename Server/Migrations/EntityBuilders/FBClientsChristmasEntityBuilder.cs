using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsChristmasEntityBuilder : FBClientsLegacyEntityBuilder<FBClientsChristmasEntityBuilder>
    {
        public FBClientsChristmasEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBClientsChristmas";
            PrimaryKey = new PrimaryKey<FBClientsChristmasEntityBuilder>("PK_GIBSFBClientsChristmas", x => x.xMasId);
            ForeignKeys.Add(new ForeignKey<FBClientsChristmasEntityBuilder>("FK_GIBSFBClientsChristmas_GIBSFBClients", x => x.FBClientsId, "GIBSFBClients", "FBClientsId", ReferentialAction.Cascade));
        }
        protected override FBClientsChristmasEntityBuilder BuildTable(ColumnsBuilder table)
        {
            xMasId = AddAutoIncrementColumn(table, "xMasId");
            FBClientsId = AddIntegerColumn(table, "FBClientsId");
            AddIntegerColumn(table, "FBClientsAddFamMemId", nullable: true);
            AddIntegerColumn(table, "LegacyXMasId", nullable: true);
            AddIntegerColumn(table, "LegacyClAddFamMemId", nullable: true);
            AddIntegerColumn(table, "xMasYear");
            AddStringColumn(table, "Sizes", 500, nullable: true, unicode: false);
            AddBooleanColumn(table, "BikeRaffle", nullable: true);
            AddDateOnlyColumn(table, "BikeAwardedDate", nullable: true);
            AddBooleanColumn(table, "WantsToys", nullable: true);
            AddBooleanColumn(table, "VerifiedToys", nullable: true);
            AddDateOnlyColumn(table, "ReceivedToysDate", nullable: true);
            AddStringColumn(table, "XmasNotes", 500, nullable: true, unicode: false);
            AddStringColumn(table, "XmasGift1", 50, nullable: true, unicode: false);
            AddStringColumn(table, "XmasGift2", 50, nullable: true, unicode: false);
            AddStringColumn(table, "XmasGift1Size", 50, nullable: true, unicode: false);
            AddStringColumn(table, "XmasGift2Size", 50, nullable: true, unicode: false);
            AddStringColumn(table, "XmasGiftRecordID", 40, nullable: true, unicode: false);
            AddLegacyAuditColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> xMasId { get; set; }
        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
    }
}
