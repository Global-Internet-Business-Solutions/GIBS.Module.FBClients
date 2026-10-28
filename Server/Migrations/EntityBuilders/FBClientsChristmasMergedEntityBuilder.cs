using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsChristmasMergedEntityBuilder : FBClientsArchiveEntityBuilder<FBClientsChristmasMergedEntityBuilder>
    {
        public FBClientsChristmasMergedEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database)
            : base(migrationBuilder, database, "GIBSFBClientsChristmasMerged")
        {
            PrimaryKey = new PrimaryKey<FBClientsChristmasMergedEntityBuilder>("PK_GIBSFBClientsChristmasMerged", x => x.ArchiveId);
            ForeignKeys.Add(new ForeignKey<FBClientsChristmasMergedEntityBuilder>("FK_GIBSFBClientsChristmasMerged_Module",
                x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        protected override FBClientsChristmasMergedEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ArchiveId = AddAutoIncrementColumn(table, "ArchiveId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            LegacyRecordId = AddIntegerColumn(table, "LegacyRecordId");
            AddArchiveAuditColumns(table);
            AddLegacyClientIdColumn(table);
            AddIntegerColumn(table, "LegacyClAddFamMemId");
            AddIntegerColumn(table, "xMasYear");
            AddStringColumn(table, "Sizes", 500, nullable: true, unicode: false);
            AddBooleanColumn(table, "BikeRaffle", nullable: true);
            AddDateOnlyColumn(table, "BikeAwardedDate", nullable: true);
            AddBooleanColumn(table, "WantsToys", nullable: true);
            AddBooleanColumn(table, "VerifiedToys", nullable: true);
            AddDateOnlyColumn(table, "ReceivedToysDate", nullable: true);
            return this;
        }

        public OperationBuilder<AddColumnOperation> ArchiveId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> LegacyRecordId { get; set; }
    }
}
