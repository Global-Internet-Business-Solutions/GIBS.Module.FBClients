using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsTrueFalseQuestionsEntityBuilder : FBClientsLegacyEntityBuilder<FBClientsTrueFalseQuestionsEntityBuilder>
    {
        public FBClientsTrueFalseQuestionsEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBClientsTrueFalseQuestions";
            PrimaryKey = new PrimaryKey<FBClientsTrueFalseQuestionsEntityBuilder>("PK_GIBSFBClientsTrueFalseQuestions", x => x.TfId);
            ForeignKeys.Add(new ForeignKey<FBClientsTrueFalseQuestionsEntityBuilder>("FK_GIBSFBClientsTrueFalseQuestions_GIBSFBClients", x => x.FBClientsId, "GIBSFBClients", "FBClientsId", ReferentialAction.Cascade));
        }
        protected override FBClientsTrueFalseQuestionsEntityBuilder BuildTable(ColumnsBuilder table)
        {
            TfId = AddAutoIncrementColumn(table, "TfId");
            FBClientsId = AddIntegerColumn(table, "FBClientsId");
            AddIntegerColumn(table, "LegacyTfId", nullable: true);
            AddStringColumn(table, "TfQuestion", 30);
            AddBooleanColumn(table, "TfAnswer", nullable: false, defaultValue: false);
            AddLegacyAuditColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> TfId { get; set; }
        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
    }
}
