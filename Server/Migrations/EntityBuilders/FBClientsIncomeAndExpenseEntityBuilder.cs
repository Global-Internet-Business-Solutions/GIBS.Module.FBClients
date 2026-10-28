using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsIncomeAndExpenseEntityBuilder : FBClientsLegacyEntityBuilder<FBClientsIncomeAndExpenseEntityBuilder>
    {
        public FBClientsIncomeAndExpenseEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBClientsIncomeAndExpense";
            PrimaryKey = new PrimaryKey<FBClientsIncomeAndExpenseEntityBuilder>("PK_GIBSFBClientsIncomeAndExpense", x => x.IeId);
            ForeignKeys.Add(new ForeignKey<FBClientsIncomeAndExpenseEntityBuilder>("FK_GIBSFBClientsIncomeAndExpense_GIBSFBClients", x => x.FBClientsId, "GIBSFBClients", "FBClientsId", ReferentialAction.Cascade));
        }
        protected override FBClientsIncomeAndExpenseEntityBuilder BuildTable(ColumnsBuilder table)
        {
            IeId = AddAutoIncrementColumn(table, "IeId");
            FBClientsId = AddIntegerColumn(table, "FBClientsId");
            AddIntegerColumn(table, "LegacyIeId", nullable: true);
            AddStringColumn(table, "IeType", 2, nullable: true);
            AddStringColumn(table, "IeDescription", 100, nullable: true);
            AddDecimalColumn(table, "IeAmount", 19, 4, nullable: true);
            AddLegacyAuditColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> IeId { get; set; }
        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
    }
}
