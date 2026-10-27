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
            FBClientsId = AddAutoIncrementColumn(table,"FBClientsId");
            ModuleId = AddIntegerColumn(table,"ModuleId");
            Name = AddMaxStringColumn(table,"Name");
            AddAuditableColumns(table);
            return this;
        }

        public OperationBuilder<AddColumnOperation> FBClientsId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> Name { get; set; }
    }
}
