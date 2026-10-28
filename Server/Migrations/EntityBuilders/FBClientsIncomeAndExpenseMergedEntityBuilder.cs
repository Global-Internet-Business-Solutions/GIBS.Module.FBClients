using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsIncomeAndExpenseMergedEntityBuilder : FBClientsArchiveEntityBuilder<FBClientsIncomeAndExpenseMergedEntityBuilder>
    {
        public FBClientsIncomeAndExpenseMergedEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database)
            : base(migrationBuilder, database, "GIBSFBClientsIncomeAndExpenseMerged")
        {
            PrimaryKey = new PrimaryKey<FBClientsIncomeAndExpenseMergedEntityBuilder>("PK_GIBSFBClientsIncomeAndExpenseMerged", x => x.ArchiveId);
            ForeignKeys.Add(new ForeignKey<FBClientsIncomeAndExpenseMergedEntityBuilder>("FK_GIBSFBClientsIncomeAndExpenseMerged_Module",
                x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        protected override FBClientsIncomeAndExpenseMergedEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ArchiveId = AddAutoIncrementColumn(table, "ArchiveId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            LegacyRecordId = AddIntegerColumn(table, "LegacyRecordId");
            AddArchiveAuditColumns(table);
            AddLegacyClientIdColumn(table);
            AddStringColumn(table, "IeType", 2, nullable: true);
            AddStringColumn(table, "IeDescription", 100, nullable: true);
            AddDecimalColumn(table, "IeAmount", 19, 4, nullable: true);
            return this;
        }

        public OperationBuilder<AddColumnOperation> ArchiveId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> LegacyRecordId { get; set; }
    }
}
