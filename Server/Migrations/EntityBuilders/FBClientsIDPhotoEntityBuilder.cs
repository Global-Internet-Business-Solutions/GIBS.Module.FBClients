using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations.EntityBuilders
{
    public class FBClientsIDPhotoEntityBuilder : FBClientsLegacyEntityBuilder<FBClientsIDPhotoEntityBuilder>
    {
        public FBClientsIDPhotoEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "GIBSFBClientsIDPhoto";
            PrimaryKey = new PrimaryKey<FBClientsIDPhotoEntityBuilder>("PK_GIBSFBClientsIDPhoto", x => x.ImageId);
            ForeignKeys.Add(new ForeignKey<FBClientsIDPhotoEntityBuilder>("FK_GIBSFBClientsIDPhoto_GIBSFBClients", x => x.FBClientsId, "GIBSFBClients", "FBClientsId", ReferentialAction.Cascade));
        }
        protected override FBClientsIDPhotoEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ImageId = AddAutoIncrementColumn(table, "ImageId");
            FBClientsId = AddIntegerColumn(table, "FBClientsId");
            AddIntegerColumn(table, "LegacyImageId", nullable: true);
            table.Column<byte[]>(name: ActiveDatabase.RewriteName("IDPhoto"), nullable: false);
            AddLegacyAuditColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> ImageId { get; set; }
        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
    }
}
