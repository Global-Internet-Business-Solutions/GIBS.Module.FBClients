using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsVisitsMergedEntityBuilder : FBClientsArchiveEntityBuilder<FBClientsVisitsMergedEntityBuilder>
    {
        public FBClientsVisitsMergedEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database)
            : base(migrationBuilder, database, "GIBSFBClientsVisitsMerged")
        {
            PrimaryKey = new PrimaryKey<FBClientsVisitsMergedEntityBuilder>("PK_GIBSFBClientsVisitsMerged", x => x.ArchiveId);
            ForeignKeys.Add(new ForeignKey<FBClientsVisitsMergedEntityBuilder>("FK_GIBSFBClientsVisitsMerged_Module",
                x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        protected override FBClientsVisitsMergedEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ArchiveId = AddAutoIncrementColumn(table, "ArchiveId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            LegacyRecordId = AddIntegerColumn(table, "LegacyRecordId");
            AddArchiveAuditColumns(table);
            AddLegacyClientIdColumn(table);
            AddDateOnlyColumn(table, "VisitDate");
            AddMaxStringColumn(table, "VisitNotes", nullable: true);
            AddIntegerColumn(table, "VisitNumBags", nullable: true);
            return this;
        }

        public OperationBuilder<AddColumnOperation> ArchiveId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> LegacyRecordId { get; set; }
    }
}
