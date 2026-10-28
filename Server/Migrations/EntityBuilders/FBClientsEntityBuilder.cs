using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsEntityBuilder : AuditableBaseEntityBuilder<FBClientsEntityBuilder>
    {
        private const string _entityTableName = "GIBSFBClients";
        private readonly PrimaryKey<FBClientsEntityBuilder> _primaryKey = new("PK_GIBSFBClients", x => x.FBClientsId);
        private readonly ForeignKey<FBClientsEntityBuilder> _moduleForeignKey = new("FK_GIBSFBClients_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade);

        public FBClientsEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = _entityTableName;
            PrimaryKey = _primaryKey;
            ForeignKeys.Add(_moduleForeignKey);
        }

        protected override FBClientsEntityBuilder BuildTable(ColumnsBuilder table)
        {
            FBClientsId = AddAutoIncrementColumn(table, "FBClientsId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            AddIntegerColumn(table, "LegacyClientId", nullable: true);
            AddIntegerColumn(table, "LegacyModuleId", nullable: true);
            AddIntegerColumn(table, "LegacyPortalId", nullable: true);
            AddIntegerColumn(table, "LegacyCaseWorkerUserId", nullable: true);
            AddDateTimeColumn(table, "LegacyCreatedOnDate", nullable: true);
            AddIntegerColumn(table, "LegacyCreatedByUserId", nullable: true);
            AddDateTimeColumn(table, "LegacyLastModifiedOnDate", nullable: true);
            AddIntegerColumn(table, "LegacyLastModifiedByUserId", nullable: true);
            AddStringColumn(table, "ClientFirstName", 30, nullable: true);
            AddStringColumn(table, "ClientMiddleInitial", 30, nullable: true);
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
            AddBooleanColumn(table, "ClientDOBVerify", nullable: false, defaultValue: false);
            AddStringColumn(table, "ClientEthnicity", 100, nullable: true);
            AddStringColumn(table, "ClientNote", 1000, nullable: true);
            AddStringColumn(table, "ClientUnit", 20, nullable: true);
            AddStringColumn(table, "ClientTown", 20, nullable: true);
            AddStringColumn(table, "ClientGender", 10, nullable: true);
            AddStringColumn(table, "ClientType", 25, nullable: true);
            AddDateOnlyColumn(table, "ClientVerifyDate", nullable: true);
            AddBooleanColumn(table, "ClientAddressVerify", nullable: false, defaultValue: false);
            AddDateOnlyColumn(table, "ClientAddressVerifyDate", nullable: true);
            AddBooleanColumn(table, "SubjectToReview", nullable: false, defaultValue: false);
            AddBooleanColumn(table, "OneBagOnly", nullable: false, defaultValue: false);
            AddStringColumn(table, "Disability", 250, nullable: true, unicode: false);
            AddBooleanColumn(table, "IsActive", nullable: true, defaultValue: true);
            AddBooleanColumn(table, "IsLocked", nullable: true, defaultValue: false);
            AddStringColumn(table, "ServiceLocation", 30, nullable: true, unicode: false);
            AddStringColumn(table, "ClientLanguage", 2, nullable: true, unicode: false);
            AddStringColumn(table, "PrimaryIncomeSource", 50, nullable: false, unicode: true, defaultValue: "");
            AddAuditableColumns(table);
            return this;
        }

        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
    }
}
