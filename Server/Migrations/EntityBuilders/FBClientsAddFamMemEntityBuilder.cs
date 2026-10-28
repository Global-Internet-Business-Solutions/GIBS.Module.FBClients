using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsAddFamMemEntityBuilder : AuditableBaseEntityBuilder<FBClientsAddFamMemEntityBuilder>
    {
        public FBClientsAddFamMemEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBClientsAddFamMem";
            PrimaryKey = new PrimaryKey<FBClientsAddFamMemEntityBuilder>("PK_GIBSFBClientsAddFamMem", x => x.FBClientsAddFamMemId);
            ForeignKeys.Add(new ForeignKey<FBClientsAddFamMemEntityBuilder>("FK_GIBSFBClientsAddFamMem_GIBSFBClients",
                x => x.FBClientsId, "GIBSFBClients", "FBClientsId", ReferentialAction.Cascade));
        }

        protected override FBClientsAddFamMemEntityBuilder BuildTable(ColumnsBuilder table)
        {
            FBClientsAddFamMemId = AddAutoIncrementColumn(table, "FBClientsAddFamMemId");
            FBClientsId = AddIntegerColumn(table, "FBClientsId");
            AddIntegerColumn(table, "LegacyClAddFamMemId", nullable: true);
            AddDateTimeColumn(table, "LegacyCreatedOnDate", nullable: true);
            AddIntegerColumn(table, "LegacyCreatedByUserId", nullable: true);
            AddDateTimeColumn(table, "LegacyLastModifiedOnDate", nullable: true);
            AddIntegerColumn(table, "LegacyLastModifiedByUserId", nullable: true);
            AddStringColumn(table, "ClAddFamMemFirstName", 50, nullable: true);
            AddStringColumn(table, "ClAddFamMemLastName", 50, nullable: true);
            AddDateOnlyColumn(table, "ClAddFamMemDOB", nullable: true);
            AddStringColumn(table, "AFMRelationship", 25, nullable: true);
            AddStringColumn(table, "AFMMiddleInitial", 15, nullable: true);
            AddStringColumn(table, "AFMSuffix", 15, nullable: true);
            AddBooleanColumn(table, "AFMDOBVerify", nullable: false, defaultValue: false);
            AddStringColumn(table, "AFMEthnicity", 100, nullable: true, unicode: false);
            AddStringColumn(table, "AFMGender", 10, nullable: true);
            AddAuditableColumns(table);
            return this;
        }

        public OperationBuilder<AddColumnOperation> FBClientsAddFamMemId { get; set; }
        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
    }
}
