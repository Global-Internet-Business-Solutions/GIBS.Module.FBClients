using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsAddFamMemMergedEntityBuilder : FBClientsArchiveEntityBuilder<FBClientsAddFamMemMergedEntityBuilder>
    {
        public FBClientsAddFamMemMergedEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database)
            : base(migrationBuilder, database, "GIBSFBClientsAddFamMemMerged")
        {
            PrimaryKey = new PrimaryKey<FBClientsAddFamMemMergedEntityBuilder>("PK_GIBSFBClientsAddFamMemMerged", x => x.ArchiveId);
            ForeignKeys.Add(new ForeignKey<FBClientsAddFamMemMergedEntityBuilder>("FK_GIBSFBClientsAddFamMemMerged_Module",
                x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        protected override FBClientsAddFamMemMergedEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ArchiveId = AddAutoIncrementColumn(table, "ArchiveId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            LegacyRecordId = AddIntegerColumn(table, "LegacyRecordId");
            AddArchiveAuditColumns(table);
            AddLegacyClientIdColumn(table);
            AddStringColumn(table, "ClAddFamMemFirstName", 50, nullable: true);
            AddStringColumn(table, "ClAddFamMemLastName", 50, nullable: true);
            AddDateOnlyColumn(table, "ClAddFamMemDOB", nullable: true);
            AddStringColumn(table, "AFMRelationship", 25, nullable: true);
            AddStringColumn(table, "AFMMiddleInitial", 15, nullable: true);
            AddStringColumn(table, "AFMSuffix", 15, nullable: true);
            AddBooleanColumn(table, "AFMDOBVerify");
            AddStringColumn(table, "AFMEthnicity", 100, nullable: true, unicode: false);
            AddStringColumn(table, "AFMGender", 10, nullable: true);
            return this;
        }

        public OperationBuilder<AddColumnOperation> ArchiveId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> LegacyRecordId { get; set; }
    }
}
