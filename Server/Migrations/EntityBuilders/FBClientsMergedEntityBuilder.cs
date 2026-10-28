using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsMergedEntityBuilder : FBClientsArchiveEntityBuilder<FBClientsMergedEntityBuilder>
    {
        public FBClientsMergedEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database)
            : base(migrationBuilder, database, "GIBSFBClientsMerged")
        {
            PrimaryKey = new PrimaryKey<FBClientsMergedEntityBuilder>("PK_GIBSFBClientsMerged", x => x.ArchiveId);
            ForeignKeys.Add(new ForeignKey<FBClientsMergedEntityBuilder>("FK_GIBSFBClientsMerged_Module",
                x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        protected override FBClientsMergedEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ArchiveId = AddAutoIncrementColumn(table, "ArchiveId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            LegacyRecordId = AddIntegerColumn(table, "LegacyRecordId");
            AddArchiveAuditColumns(table);
            AddIntegerColumn(table, "LegacyModuleId");
            AddIntegerColumn(table, "LegacyPortalId", nullable: true);
            AddIntegerColumn(table, "LegacyCaseWorkerUserId", nullable: true);
            AddIntegerColumn(table, "LegacyMergedWithClientId", nullable: true);
            AddStringColumn(table, "ClientFirstName", 30, nullable: true);
            AddStringColumn(table, "ClientMiddleInitial", 15, nullable: true);
            AddStringColumn(table, "ClientLastName", 30, nullable: true);
            AddDateOnlyColumn(table, "ClientDOB", nullable: true);
            AddStringColumn(table, "ClientAddress", 50, nullable: true);
            AddStringColumn(table, "ClientCity", 50, nullable: true);
            AddStringColumn(table, "ClientState", 20, nullable: true);
            AddStringColumn(table, "ClientZipCode", 10, nullable: true);
            AddStringColumn(table, "ClientEmailAddress", 50, nullable: true);
            AddStringColumn(table, "ClientIdCard", 50, nullable: true);
            AddStringColumn(table, "ClientPhone", 25, nullable: true);
            AddStringColumn(table, "ClientPhoneType", 25, nullable: true);
            AddDecimalColumn(table, "Latitude", 10, 5, nullable: true);
            AddDecimalColumn(table, "Longitude", 10, 5, nullable: true);
            table.Column<byte[]>(name: ActiveDatabase.RewriteName("ClientPhoto"), nullable: true);
            AddStringColumn(table, "ClientSuffix", 10, nullable: true);
            AddBooleanColumn(table, "ClientDOBVerify");
            AddStringColumn(table, "ClientEthnicity", 100, nullable: true);
            AddStringColumn(table, "ClientNote", 1000, nullable: true);
            AddStringColumn(table, "ClientUnit", 20, nullable: true);
            AddStringColumn(table, "ClientTown", 20, nullable: true);
            AddStringColumn(table, "ClientGender", 10, nullable: true);
            AddStringColumn(table, "ClientType", 25, nullable: true);
            AddDateOnlyColumn(table, "ClientVerifyDate", nullable: true);
            AddBooleanColumn(table, "ClientAddressVerify");
            AddDateOnlyColumn(table, "ClientAddressVerifyDate", nullable: true);
            AddBooleanColumn(table, "SubjectToReview");
            return this;
        }

        public OperationBuilder<AddColumnOperation> ArchiveId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> LegacyRecordId { get; set; }
    }
}
