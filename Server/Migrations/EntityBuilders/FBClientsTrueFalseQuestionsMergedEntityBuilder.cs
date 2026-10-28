using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsTrueFalseQuestionsMergedEntityBuilder : FBClientsArchiveEntityBuilder<FBClientsTrueFalseQuestionsMergedEntityBuilder>
    {
        public FBClientsTrueFalseQuestionsMergedEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database)
            : base(migrationBuilder, database, "GIBSFBClientsTrueFalseQuestionsMerged")
        {
            PrimaryKey = new PrimaryKey<FBClientsTrueFalseQuestionsMergedEntityBuilder>("PK_GIBSFBClientsTrueFalseQuestionsMerged", x => x.ArchiveId);
            ForeignKeys.Add(new ForeignKey<FBClientsTrueFalseQuestionsMergedEntityBuilder>("FK_GIBSFBClientsTrueFalseQuestionsMerged_Module",
                x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        protected override FBClientsTrueFalseQuestionsMergedEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ArchiveId = AddAutoIncrementColumn(table, "ArchiveId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            LegacyRecordId = AddIntegerColumn(table, "LegacyRecordId");
            AddArchiveAuditColumns(table);
            AddLegacyClientIdColumn(table);
            AddStringColumn(table, "TfQuestion", 30);
            AddBooleanColumn(table, "TfAnswer");
            return this;
        }

        public OperationBuilder<AddColumnOperation> ArchiveId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> LegacyRecordId { get; set; }
    }
}
