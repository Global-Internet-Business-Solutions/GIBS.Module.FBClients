using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBTurkeyRecipientEntityBuilder : AuditableBaseEntityBuilder<FBTurkeyRecipientEntityBuilder>
    {
        public FBTurkeyRecipientEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBTurkeyRecipients";
            PrimaryKey = new PrimaryKey<FBTurkeyRecipientEntityBuilder>("PK_GIBSFBTurkeyRecipients", x => x.TurkeyId);
            ForeignKeys.Add(new ForeignKey<FBTurkeyRecipientEntityBuilder>("FK_GIBSFBTurkeyRecipients_GIBSFBClients", x => x.FBClientsId, "GIBSFBClients", "FBClientsId", ReferentialAction.Cascade));
        }

        protected override FBTurkeyRecipientEntityBuilder BuildTable(ColumnsBuilder table)
        {
            TurkeyId = AddAutoIncrementColumn(table, "TurkeyId");
            FBClientsId = AddIntegerColumn(table, "FBClientsId");
            AddIntegerColumn(table, "Year");
            AddStringColumn(table, "Holiday", 50, nullable: true, unicode: false);
            AddDateTimeColumn(table, "LegacyCreatedOnDate", nullable: true);
            AddAuditableColumns(table);
            return this;
        }

        public OperationBuilder<AddColumnOperation> TurkeyId { get; set; }
        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
    }
}
